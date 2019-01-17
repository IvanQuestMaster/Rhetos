/*
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
    [ExportMetadata(MefProvider.Implements, typeof(LegacyPropertySimpleInfo))]
    public class LegacyPropertySimpleDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        private readonly ISqlUtility _sqlUtility;

        public LegacyPropertySimpleDatabaseDefinition(ISqlUtility sqlUtility)
        {
            _sqlUtility = sqlUtility;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (LegacyPropertySimpleInfo) conceptInfo;
            createdDependencies = null;

            codeBuilder.InsertCode(Sql.Format("LegacyPropertySimpleDatabaseDefinition_ExtendViewSelect", _sqlUtility.Identifier(info.Property.Name), _sqlUtility.Identifier(info.Column)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.ViewSelectPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            codeBuilder.InsertCode(Sql.Format("LegacyPropertySimpleDatabaseDefinition_ExtendTriggerInsert", _sqlUtility.Identifier(info.Column)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            codeBuilder.InsertCode(Sql.Format("LegacyPropertySimpleDatabaseDefinition_ExtendTriggerSelectForInsert", _sqlUtility.Identifier(info.Column), _sqlUtility.Identifier(info.Property.Name)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            codeBuilder.InsertCode(Sql.Format("LegacyPropertySimpleDatabaseDefinition_ExtendTriggerSelectForUpdate", _sqlUtility.Identifier(info.Column), _sqlUtility.Identifier(info.Property.Name)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForUpdatePartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

        }
    }
}
