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
    public class DslSubsetTest
    {
        #region Sample concept classes

        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public string Data { get; set; }
        }

        class SimpleConcept2Info : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        #endregion

        class TestDslTracker : IDslTracker
        {
            public List<IConceptInfo> TrackedConcepts { get; private set; }

            public TestDslTracker()
            {
                TrackedConcepts = new List<IConceptInfo>();
            }

            public void AddConceptToTracker(IConceptInfo conceptInfo)
            {
                TrackedConcepts.Add(conceptInfo);
            }
        }

        [TestMethod]
        public void SimpleTrackingTest()
        {
            var conceptList = new List<IConceptInfo> {
                new SimpleConceptInfo{ Name = "Test1", Data = "Test1.1"},
                new SimpleConceptInfo{ Name = "Test2", Data = "Test2.1"},
            };

            var dslTracker = new TestDslTracker();
            var dslSubset = new DslSubset<IConceptInfo>(dslTracker, conceptList);

            var result = dslSubset.OfType<SimpleConceptInfo>().Where(x => x.Data == "Test2.1").ToList();
            Assert.AreEqual("SimpleConceptInfo Test2", dslTracker.TrackedConcepts.Single().GetKey());
        }
    }
}
