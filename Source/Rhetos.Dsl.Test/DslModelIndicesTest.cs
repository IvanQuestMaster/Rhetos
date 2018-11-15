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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class DslModelIndices
    {
        #region Sample concept classes

        class SimpleReferenceConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public SimpleConceptInfo Reference1 { get; set; }
        }

        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        #endregion

        [TestMethod]
        public void DslModelIndexByReferenceRemoveReferencedTest()
        {
            var indexByReference = new DslModelIndexByReference();

            var simpleConcept1 = new SimpleConceptInfo { Name = "Concept1" };
            var referenceConcept1 = new SimpleReferenceConceptInfo { Reference1 = simpleConcept1 };
            var referenceConcept2 = new SimpleReferenceConceptInfo { Reference1 = simpleConcept1 };

            indexByReference.Add(simpleConcept1);
            indexByReference.Add(referenceConcept1);
            indexByReference.Add(referenceConcept2);

            var concepts = indexByReference.FindByReference(typeof(SimpleReferenceConceptInfo), false, "Reference1", simpleConcept1.GetKey()).ToList();
            Assert.AreEqual(2, concepts.Count);
            Assert.AreEqual(referenceConcept1.GetKey(), concepts[0].GetKey());
            Assert.AreEqual(referenceConcept2.GetKey(), concepts[1].GetKey());

            indexByReference.Remove(simpleConcept1);

            var concepts2 = indexByReference.FindByReference(typeof(SimpleReferenceConceptInfo), false, "Reference1", simpleConcept1.GetKey());
            Assert.AreEqual(0, concepts2.Count());
        }

        [TestMethod]
        public void DslModelIndexByReferenceRemoveSourceTest()
        {
            var indexByReference = new DslModelIndexByReference();

            var simpleConcept1 = new SimpleConceptInfo { Name = "Concept1" };
            var referenceConcept1 = new SimpleReferenceConceptInfo { Reference1 = simpleConcept1 };

            indexByReference.Add(simpleConcept1);
            indexByReference.Add(referenceConcept1);

            var concepts = indexByReference.FindByReference(typeof(SimpleReferenceConceptInfo), false, "Reference1", simpleConcept1.GetKey()).ToList();
            Assert.AreEqual(referenceConcept1.GetKey(), concepts.Single().GetKey());

            indexByReference.Remove(referenceConcept1);

            var concepts2 = indexByReference.FindByReference(typeof(SimpleReferenceConceptInfo), false, "Reference1", simpleConcept1.GetKey());
            Assert.AreEqual(0, concepts2.Count());
        }
    }
}
