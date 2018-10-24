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

        class SimpleConceptWithCodeSnippetInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            [CodeSnippet]
            public string CodeSnippet { get; set; }
        }

        [TestMethod]
        public void SimpleDiffWithCodeSnippets()
        {
            var oldConcepts = new List<IConceptInfo>
            {
                new SimpleConceptWithCodeSnippetInfo { Name = "Simple 1", CodeSnippet = "" },
                new SimpleConceptWithCodeSnippetInfo { Name = "Simple 2", CodeSnippet = "Code snippet 1" },
            };

            var newConcepts = new List<IConceptInfo>
            {
                new SimpleConceptWithCodeSnippetInfo { Name = "Simple 2", CodeSnippet = "Code snippet 2" },
                new SimpleConceptWithCodeSnippetInfo { Name = "Simple 3", CodeSnippet = "" },
            };

            var diffResults = oldConcepts.Diff(newConcepts, true);
            Assert.AreEqual(oldConcepts[0].GetKey(), diffResults.Removed.Single().GetKey());
            Assert.AreEqual(0, diffResults.Changed.Count());
            Assert.AreEqual(newConcepts[1].GetKey(), diffResults.Added.Single().GetKey());
        }

        class SimpleConcept2Info : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            [ConceptKey]
            [CodeSnippet]
            public string CodeSnippet { get; set; }
        }

        [TestMethod]
        public void SimpleDiffWithCodeSnippetsAndConcetpKeyOnSameProperty()
        {
            var oldConcepts = new List<IConceptInfo>
            {
                new SimpleConcept2Info { Name = "Simple", CodeSnippet = "Code snippet 1" },
            };

            var newConcepts = new List<IConceptInfo>
            {
                new SimpleConcept2Info { Name = "Simple", CodeSnippet = "Code snippet 2" }
            };

            var diffResults = oldConcepts.Diff(newConcepts, true);
            Assert.AreEqual(oldConcepts[0].GetKey(), diffResults.Removed.Single().GetKey());
            Assert.AreEqual(newConcepts[0].GetKey(), diffResults.Added.Single().GetKey());
        }
    }
}
