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
            {
                var foundOption = false;
                var result = new string[] { "-o", "test", "--long-option" }.GetOption("long-option|short-option", hasOption => foundOption = hasOption);
                Assert.AreEqual(true, foundOption);
            }

            {
                var foundOption = false;
                var result = new string[] { "test", "-short-option" }.GetOption("long-option|short-option", hasOption => foundOption = hasOption);
                Assert.AreEqual(true, foundOption);
            }

            {
                var foundOption = false;
                var result = new string[] { "test", "-short-option", "-test", "some-value", "-short-option" }.GetOption("long-option|short-option", hasOption => foundOption = hasOption);
                Assert.AreEqual(true, foundOption);
            }
        }

        [TestMethod]
        public void MultipleSameOptionTest()
        {
            var foundOption = false;
            var result = new string[] { "--long-option", "--other-value", "test", "-short-option", "-test", "some-value", "-short-option" }.GetOption("long-option|short-option", hasOption => foundOption = hasOption);
            Assert.AreEqual(true, foundOption);
        }

        [TestMethod]
        public void ShortAndLongOptionNotFoundTest()
        {
            {
                var foundOption = false;
                var result = new string[] { "-o", "test", "-long-option" }.GetOption("long-option|short-option", hasOption => foundOption = hasOption);
                Assert.AreEqual(false, foundOption, "The long option should not be found because it starts with \"-\"");
            }

            {
                var foundOption = false;
                var result = new string[] { "-o", "test", "--short-option" }.GetOption("long-option|short-option", hasOption => foundOption = hasOption);
                Assert.AreEqual(false, foundOption, "The short option should not be found because it starts with \"--\"");
            }

            {
                var foundOption = false;
                var result = new string[] { "-long-option", "-o", "test", "--short-option" }.GetOption("long-option|short-option", hasOption => foundOption = hasOption);
                Assert.AreEqual(false, foundOption, "The short option should not be found because the long option should start with \"--\" and short option with\"-\"");
            }
        }

        [TestMethod]
        public void GetOptionValueTest()
        {
            var value = "";
            new string[] {"--option-with-value", "1" }.GetOptionValue("option-with-value", optionValue => value = optionValue);
            Assert.AreEqual("1", value);
        }

        [TestMethod]
        public void GetOptionWithMultipleValuesTest()
        {
            var value = "";
            TestUtility.ShouldFail<ArgumentException>(() => {
                new string[] { "--option-with-value", "1", "2" }.GetOptionValue("option-with-value", optionValue => value = optionValue);
            }, "option-with-value", "should only have one value");
        }

        [TestMethod]
        public void GetOptionWithoutValueTest()
        {
            var value = "";
            TestUtility.ShouldFail<ArgumentException>(() => {
                new string[] { "--option-with-value", "--other-option"}.GetOptionValue("option-with-value", optionValue => value = optionValue);
            }, "option-with-value", "should contain a value");
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
