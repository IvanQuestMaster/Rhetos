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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(LegacyPropertyReferenceInfo))]
    public class LegacyPropertyReferenceDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        private readonly ISqlUtility _sqlUtility;
        private readonly ISqlResourceProvider _sql;

        public LegacyPropertyReferenceDatabaseDefinition(ISqlUtility sqlUtility, ISqlResourceProvider sql)
        {
            _sqlUtility = sqlUtility;
            _sql = sql;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        private static int _uniqueNum = 1;

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (LegacyPropertyReferenceInfo) conceptInfo;
            createdDependencies = null;

            var sourceColumns = info.Columns.Split(',').Select(s => s.Trim()).Select(s => _sqlUtility.Identifier(s)).ToArray();
            var refColumns = info.ReferencedColumns.Split(',').Select(s => s.Trim()).Select(s => _sqlUtility.Identifier(s)).ToArray();
            if (sourceColumns.Length != refColumns.Length)
                throw new DslSyntaxException("Count of references columns does not match count of source columns in " + info.GetUserDescription() + ". "
                    + "There are " + sourceColumns.Length + " source columns and " + refColumns.Length + " referenced columns.");

            string refAlias = _sqlUtility.Identifier("ref" + _uniqueNum++);

            // Add column to view:

            codeBuilder.InsertCode(_sql.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendViewSelect", _sqlUtility.Identifier(info.Property.Name), refAlias),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.ViewSelectPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            var allColumnsEqual = string.Join(" AND ", sourceColumns.Zip(refColumns,
                (sCol, rCol) => _sql.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendFromJoin", refAlias, rCol, sCol)));
            codeBuilder.InsertCode(_sql.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendViewFrom", _sqlUtility.GetFullName(info.ReferencedTable), refAlias, allColumnsEqual),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.ViewFromPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            // Add columns to instead-of trigger:

            foreach (var fkColumn in sourceColumns.Zip(refColumns, Tuple.Create))
            {
                codeBuilder.InsertCode(_sql.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerInsert", fkColumn.Item1),
                    LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

                codeBuilder.InsertCode(_sql.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerSelectForInsert",
                        fkColumn.Item1,
                        refAlias,
                        fkColumn.Item2,
                        _sqlUtility.GetFullName(info.ReferencedTable),
                        _sqlUtility.Identifier(info.Property.Name)),
                    LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

                codeBuilder.InsertCode(_sql.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerSelectForUpdate",
                        fkColumn.Item1,
                        refAlias,
                        fkColumn.Item2,
                        _sqlUtility.GetFullName(info.ReferencedTable),
                        _sqlUtility.Identifier(info.Property.Name)),
                    LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForUpdatePartTag, info.Dependency_LegacyEntityWithAutoCreatedView);
            }

            codeBuilder.InsertCode(
                _sql.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerFrom", _sqlUtility.GetFullName(info.ReferencedTable), refAlias, _sqlUtility.Identifier(info.Property.Name)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerFromPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);
        }
    }
}