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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class ConceptsDiffTest
    {
        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            public string Data { get; set; }
        }

        [TestMethod]
        public void SimpleDiff()
        {
            var oldConcepts = new List<IConceptInfo>
            {
                new SimpleConceptInfo { Name = "Simple 1", Data = "Simple 1" },
                new SimpleConceptInfo { Name = "Simple 2", Data = "Simple 2" },
            };

            var newConcepts = new List<IConceptInfo>
            {
                new SimpleConceptInfo { Name = "Simple 2", Data = "Simple 2.1" },
                new SimpleConceptInfo { Name = "Simple 3", Data = "Simple 3" },
            };

            var diffResults = oldConcepts.Diff(newConcepts);
            Assert.AreEqual(oldConcepts[0].GetKey(), diffResults.Removed.Single().GetKey());
            Assert.AreEqual(oldConcepts[1].GetKey(), diffResults.Changed.Single().GetKey());
            Assert.AreEqual("Simple 2.1", (diffResults.Changed.Single() as SimpleConceptInfo).Data);
            Assert.AreEqual(newConcepts[1].GetKey(), diffResults.Added.Single().GetKey());
        }
    }
}
