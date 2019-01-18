﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueReferenceInfo))]
    public class UniqueReferenceDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<UniqueReferenceInfo> ForeignKeyConstraintOptionsTag = "FK options";

        private readonly ISqlUtility _sqlUtility;
        private readonly ISqlResourceProvider _sql;

        public UniqueReferenceDatabaseDefinition(ISqlUtility sqlUtility, ISqlResourceProvider sql)
        {
            _sqlUtility = sqlUtility;
            _sql = sql;
        }

        [Obsolete]
        public static string GetConstraintName(UniqueReferenceInfo info)
        {
            return SqlUtility.Identifier(Sql.Format("UniqueReferenceDatabaseDefinition_ConstraintName",
                info.Extension.Name,
                info.Base.Name));
        }

        public static string GetConstraintName(UniqueReferenceInfo info, ISqlUtility sqlUtility, ISqlResourceProvider sql)
        {
            return sqlUtility.Identifier(sql.Format("UniqueReferenceDatabaseDefinition_ConstraintName",
                info.Extension.Name,
                info.Base.Name));
        }

        public static bool ShouldCreateConstraint(UniqueReferenceInfo info)
        {
            return info.Extension is EntityInfo
                && ForeignKeyUtility.GetSchemaTableForForeignKey(info.Base) != null;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (UniqueReferenceInfo)conceptInfo;
            if (ShouldCreateConstraint(info))
            {
                return _sql.Format("UniqueReferenceDatabaseDefinition_Create",
                    _sqlUtility.Identifier(info.Extension.Module.Name) + "." + _sqlUtility.Identifier(info.Extension.Name),
                    GetConstraintName(info, _sqlUtility, _sql),
                    ForeignKeyUtility.GetSchemaTableForForeignKey(info.Base, _sqlUtility),
                    ForeignKeyConstraintOptionsTag.Evaluate(info));
            }
            // TODO: else - Generate a Filter+InvalidData validation in the server application that checks for invalid items.
            return "";
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (UniqueReferenceInfo) conceptInfo;

            var dependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();
            dependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(info.Base, info.Extension));

            if (ShouldCreateConstraint(info))
                dependencies.AddRange(ForeignKeyUtility.GetAdditionalForeignKeyDependencies(info.Base)
                    .Select(dep => Tuple.Create<IConceptInfo, IConceptInfo>(dep, info))
                    .ToList());

            createdDependencies = dependencies;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (UniqueReferenceInfo)conceptInfo;
            if (ShouldCreateConstraint(info))
            {
                return _sql.Format("UniqueReferenceDatabaseDefinition_Remove",
                    _sqlUtility.Identifier(info.Extension.Module.Name) + "." + _sqlUtility.Identifier(info.Extension.Name),
                    GetConstraintName(info, _sqlUtility, _sql));
            }
            return "";
        }
    }
}
