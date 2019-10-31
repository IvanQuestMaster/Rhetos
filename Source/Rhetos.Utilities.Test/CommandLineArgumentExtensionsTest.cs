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

using Rhetos.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using Rhetos.TestCommon;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class CommandLineArgumentExtensionsTest
    {
        [TestMethod]
        public void TakeRangesTest()
        {
            var result = new string[] { "range-start", "1", "2", "range-end", "range-start", "range-end" }.
                TakeRanges(x => x == "range-start", x => x == "range-end").ToList();
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("range-start,1,2", string.Join(",", result[0]));
            Assert.AreEqual("range-start", string.Join(",", result[1]));
        }

        [TestMethod]
        public void TakeRangesWithNoEndTest()
        {
            var result = new string[] { "range-start", "1", "2" }.
                TakeRanges(x => x == "range-start", x => x == "range-end").ToList();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("range-start,1,2", string.Join(",", result[0]));
        }

        [TestMethod]
        public void TakeRangesOnlyEndElementOccurenceTest()
        {
            var result = new string[] {"1", "2", "range-end" }.
                TakeRanges(x => x == "range-start", x => x == "range-end").ToList();
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void HasOptionTest()
        {
            var foundOption = false;
            var result = new string[] { "-o", "test", "--long-option" }.HasOption("long-option", hasOption => foundOption = hasOption);
            Assert.AreEqual(true, foundOption);

            var foundOption2 = false;
            var result2 = new string[] { "test", "-short-option" }.HasOption("long-option|short-option", hasOption => foundOption2 = hasOption);
            Assert.AreEqual(true, foundOption2);

            var foundOption3 = false;
            var result3 = new string[] { "test", "-short-option", "test", "-short-option" }.HasOption("long-option|short-option", hasOption => foundOption3 = hasOption);
            Assert.AreEqual(true, foundOption3);
        }

        [TestMethod]
        public void GetOptionValueTest()
        {
            var value = "";
            new string[] {"--option-with-value", "1" }.GetOptionValue("option-with-value", optionValue => value = optionValue);
            Assert.AreEqual("1", value);
        }

        [TestMethod]
        public void GetOptionValuesTest()
        {
            var value = new string[0];
            new string[] { "--option-with-value", "1", "2", "--option-with-value", "3" }.GetOptionValues("option-with-value", optionValues => value = optionValues);
            Assert.AreEqual("1,2,3", string.Join(",", value));
        }
    }
}
