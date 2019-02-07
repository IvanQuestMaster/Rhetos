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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Unique")]
    public class UniquePropertiesInfo : IValidationConcept, IMacroConcept2
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }
        [ConceptKey]
        public PropertyInfo Property1 { get; set; }
        [ConceptKey]
        public PropertyInfo Property2 { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IDslModel existingConcepts)
        {
            return new[] { new UniqueMultiplePropertiesInfo { DataStructure = DataStructure, PropertyNames = Property1.Name + " " + Property2.Name } };
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property1, DataStructure, this);
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property2, DataStructure, this);
        }
    }
}
