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
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DefaultValueInfo))]
    public class DefaultValueCodeGenerator : IConceptCodeGenerator
    {
        /// <summary>
        /// Inserted code should be formatted "if (...) ... else".
        /// </summary>
        public static readonly CsTag<DefaultValueInfo> DefaultValueOverrideTag = new CsTag<DefaultValueInfo>("DefaultValueOverride", TagType.Reverse);

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DefaultValueInfo)conceptInfo;
            codeBuilder.InsertCode(GenerateFuncAndCallForProperty(info), WritableOrmDataStructureCodeGenerator.InitializationTag, info.Property.DataStructure);
        }

        private string GenerateFuncAndCallForProperty(DefaultValueInfo info)
        {
            var propertyName = info.Property is ReferencePropertyInfo ? info.Property.Name + "ID" : info.Property.Name;
            return $@"{{
                var defaultValue_{propertyName} = Function<{info.Property.DataStructure.FullName}>.Create({info.Expression});

                foreach (var item in insertedNew)
                    {DefaultValueOverrideTag.Evaluate(info)}
                    if (item.{propertyName} == null)
                        item.{propertyName} = defaultValue_{propertyName}(item);
            }}

            ";
        }
    }
}
