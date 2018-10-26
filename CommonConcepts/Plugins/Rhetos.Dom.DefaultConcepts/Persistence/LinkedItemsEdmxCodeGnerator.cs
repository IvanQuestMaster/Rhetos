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
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Persistence;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IEdmxCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(LinkedItemsInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureEdmxCodeGenerator))]
    public class LinkedItemsEdmxCodeGenerator : IEdmxCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var linkedItemsInfo = conceptInfo as LinkedItemsInfo;

            if (linkedItemsInfo.DataStructure is IOrmDataStructure)
            {
                codeBuilder.InsertCode(GetNavigationPropertyNodeForConceptualModel(linkedItemsInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(linkedItemsInfo.DataStructure));
            }
        }
        private static string GetNavigationPropertyNodeForConceptualModel(LinkedItemsInfo linkedItemsInfo)
        {
            return "\n" + $@"    <NavigationProperty FromRole=""{GetName(linkedItemsInfo)}_Target"" Name=""{linkedItemsInfo.Name}"" Relationship=""Self.{ GetName(linkedItemsInfo)}"" ToRole=""{ GetName(linkedItemsInfo)}_Source"" />";
        }

        private static string GetName(LinkedItemsInfo linkedItemsInfo)
        {
            return linkedItemsInfo.DataStructure.Module + "_" + linkedItemsInfo.ReferenceProperty.DataStructure.Name + "_" + linkedItemsInfo.DataStructure.Name;
        }

        private static string GetAssociationSetNameForRefrence(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"{referencePropertyInfo.DataStructure.Module}_{referencePropertyInfo.DataStructure.Name}_{referencePropertyInfo.Name}";
        }

        private static string GetEFName(DataStructureInfo dataStructureInfo)
        {
            return $@"{dataStructureInfo.Module}_{dataStructureInfo.Name}";
        }
    }
}
