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

using Microsoft.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.TestCommon;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class MarkedCodeTest
    {
        [ConceptKeyword("SimpleConcept")]
        private class SimpleConcept : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            public string Code { get; set; }
        }

        [TestMethod]
        public void MarkedCodeWithoutMarkers()
        {
            var codeWithoutMarker = "Should retunr the string as it is";
            var codeMarker = new MarkedCode(codeWithoutMarker);
            Assert.AreEqual(codeWithoutMarker, codeMarker.StrippedCode);
        }

        [TestMethod]
        public void StrippedCode()
        {
            var simpleConceptInstance = new SimpleConcept { Name = "Test", Code = "Some code" };
            var codeWithoutMarker = $@"Code before {CsMarker.GenerateMarker(simpleConceptInstance, x => x.Code)} Code after";
            var codeMarker = new MarkedCode(codeWithoutMarker);
            Assert.AreEqual("Code before Some code Code after", codeMarker.StrippedCode);
        }

        [TestMethod]
        public void GetCodeOffsetSimple()
        {
            var simpleConceptInstance = new SimpleConcept { Name="Test", Code = "Some code" };
            var code = "Doing something\nMore code " + CsMarker.GenerateMarker(simpleConceptInstance, x => x.Code) + "\nDoing something else";
            var codeMarker = new MarkedCode(code);

            //Check he beginning of the code snippet
            var errorOffset = codeMarker.GetNearestMarker(2, 11);
            Assert.AreEqual(0, errorOffset.Offset);
            Assert.AreEqual(simpleConceptInstance.GetKey(), errorOffset.ConceptKey);

            var errorOffset2 = codeMarker.GetNearestMarker(2, 19);
            Assert.AreEqual(8, errorOffset2.Offset);
            Assert.AreEqual(simpleConceptInstance.GetKey(), errorOffset2.ConceptKey);

            //Should include the position after the end of the code snippet
            var errorOffset3 = codeMarker.GetNearestMarker(2, 20);
            Assert.AreEqual(9, errorOffset3.Offset);
            Assert.AreEqual(simpleConceptInstance.GetKey(), errorOffset3.ConceptKey);
        }

        [TestMethod]
        public void GetCodeOffsetNested()
        {
            var simpleConceptInstance1 = new SimpleConcept { Name = "Test", Code = "Some code" };
            var code1 = "Doing something\nMore code " +
                CsMarker.GenerateMarker(simpleConceptInstance1, x => x.Code) +
                "\nDoing something else";
            var simpleConceptInstance2 = new SimpleConcept { Name = "Test1", Code = code1 };
            var code2 = "Doing something 2\nMore code " +
                CsMarker.GenerateMarker(simpleConceptInstance2, x => x.Code) +
                "\nDoing something else 2";
            var codeMarker = new MarkedCode(code2);
            var errorOffset = codeMarker.GetNearestMarker(3, 11);
            Assert.AreEqual(0, errorOffset.Offset);
            Assert.AreEqual(simpleConceptInstance1.GetKey(), errorOffset.ConceptKey);
        }

        [TestMethod]
        public void GetNewLines()
        {
            var code = "Line 1\nAnother line\nLine 3";
            var lines = MarkedCode.GetNewlines(code);

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual(new MarkedCode.LinePosition { Start = 0, Length = 6}, lines[0]);
            Assert.AreEqual(new MarkedCode.LinePosition { Start = 7, Length = 12 }, lines[1]);
            Assert.AreEqual(new MarkedCode.LinePosition { Start = 20, Length = 6 }, lines[2]);
        }
    }
}
