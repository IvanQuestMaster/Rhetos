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

using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Rhetos.TestCommon;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Rhetos.Dsl.Test
{

    [TestClass]
    public class ConceptInfoHelperTest
    {
        #region Sample concept classes

        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public string Data { get; set; }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public SimpleConceptInfo() { }

            public SimpleConceptInfo(string name, string data)
            {
                Name = name;
                Data = data;
            }
        }

        class DerivedConceptInfo : SimpleConceptInfo
        {
            public string Extra { get; set; }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public DerivedConceptInfo(string name, string data, string extra)
                : base(name, data)
            {
                Extra = extra;
            }
        }

        class DerivedWithKeyInfo : SimpleConceptInfo
        {
            [ConceptKey]
            public string Extra { get; set; }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public DerivedWithKeyInfo(string name, string data, string extra)
                : base(name, data)
            {
                Extra = extra;
            }
        }

        class RefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public SimpleConceptInfo Reference { get; set; }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
            public RefConceptInfo() { }
            public RefConceptInfo(string name, SimpleConceptInfo reference)
            {
                Name = name;
                Reference = reference;
            }
        }

        class RefRefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public RefConceptInfo Reference { get; set; }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
            public RefRefConceptInfo() { }
            public RefRefConceptInfo(string name, RefConceptInfo reference)
            {
                Name = name;
                Reference = reference;
            }
        }

        class RefIntConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public IConceptInfo Reference { get; set; }
        }

        #endregion

        //=========================================================================


        [TestMethod]
        public void GetKey_Simple()
        {
            Assert.AreEqual("SimpleConceptInfo a", new SimpleConceptInfo("a", "b").GetKey());
            Assert.AreEqual("SimpleConceptInfo 'a x'", new SimpleConceptInfo("a x", "b").GetKey());
            Assert.AreEqual("SimpleConceptInfo ''", new SimpleConceptInfo("", "b").GetKey());
            Assert.AreEqual("SimpleConceptInfo '\"'", new SimpleConceptInfo("\"", "b").GetKey(), "Should use single quotes when text contains double quotes.");
            Assert.AreEqual("SimpleConceptInfo \"'\"", new SimpleConceptInfo("'", "b").GetKey(), "Should use double quotes when text contains single quotes.");
            Assert.AreEqual("SimpleConceptInfo '''\"'", new SimpleConceptInfo("'\"", "b").GetKey(), "Should use single quote when text contains both quotes.");
            Assert.AreEqual("SimpleConceptInfo a123_a", new SimpleConceptInfo("a123_a", "b").GetKey());
        }

        [TestMethod]
        public void GetKey_Derived()
        {
            Assert.AreEqual("SimpleConceptInfo a", new DerivedConceptInfo("a", "b", "c").GetKey());
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void GetKey_DerivationMustNotHaveKey()
        {
            try
            {
                new DerivedWithKeyInfo("a", "b", "c").GetKey();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("DerivedWithKeyInfo"));
                Assert.IsTrue(ex.Message.Contains("Extra"));
                throw;
            }
        }

        [TestMethod]
        public void GetKey_Reference()
        {
            Assert.AreEqual(
                "RefConceptInfo a.b",
                new RefConceptInfo("a", new SimpleConceptInfo("b", "c")).GetKey());
        }

        [TestMethod]
        public void GetKey_ReferenceToInterface()
        {
            Assert.AreEqual(
                "RefIntConceptInfo a.SimpleConceptInfo:b",
                new RefIntConceptInfo { Name = "a", Reference = new DerivedConceptInfo("b", "c", "d")}.GetKey());
        }

        //=========================================================================


        [TestMethod]
        public void GetKeyProperties_Reference()
        {
            Assert.AreEqual(
                "a.b",
                new RefConceptInfo("a", new SimpleConceptInfo("b", "c")).GetKeyProperties());
        }

        //=========================================================================


        [TestMethod]
        public void GetFullDescription_Simple()
        {
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+SimpleConceptInfo a b", new SimpleConceptInfo("a", "b").GetFullDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+SimpleConceptInfo \"'\" ''", new SimpleConceptInfo("'", "").GetFullDescription());
        }

        [TestMethod]
        public void GetFullDescription_Derived()
        {
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+DerivedConceptInfo a b c", new DerivedConceptInfo("a", "b", "c").GetFullDescription());
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void GetFullDescription_DerivationMustNotHaveKey()
        {
            try
            {
                new DerivedWithKeyInfo("a", "b", "c").GetFullDescription();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("DerivedWithKeyInfo"));
                Assert.IsTrue(ex.Message.Contains("Extra"));
                throw;
            }
        }

        [TestMethod]
        public void GetFullDescription_ReferencedConceptIsDescribedWithKeyOnly()
        {
            Assert.AreEqual(
                "Rhetos.Dsl.Test.ConceptInfoHelperTest+RefConceptInfo a.b",
                new RefConceptInfo("a", new DerivedConceptInfo("b", "c", "d")).GetFullDescription());
        }

        //=========================================================================

        [TestMethod]
        public void GetDirectDependencies_Empty()
        {
            var conceptInfo = new SimpleConceptInfo("s", "d");
            var dependencies = conceptInfo.GetDirectDependencies();
            Assert.AreEqual("()", Dump(dependencies));
        }

        [TestMethod]
        public void GetDirectDependencies_NotRecursive()
        {
            var simpleConceptInfo = new SimpleConceptInfo("s", "d");
            var refConceptInfo = new RefConceptInfo("r", simpleConceptInfo);
            var refRefConceptInfo = new RefRefConceptInfo("rr", refConceptInfo);

            var dependencies = refRefConceptInfo.GetDirectDependencies();
            Assert.AreEqual(Dump(new IConceptInfo[] { refConceptInfo }), Dump(dependencies));
        }

        [TestMethod]
        public void GetAllDependencies_Recursive()
        {
            var simpleConceptInfo = new SimpleConceptInfo("s", "d");
            var refConceptInfo = new RefConceptInfo("r", simpleConceptInfo);
            var refRefConceptInfo = new RefRefConceptInfo("rr", refConceptInfo);

            var dependencies = refRefConceptInfo.GetAllDependencies();
            Assert.AreEqual(Dump(new IConceptInfo[] { simpleConceptInfo, refConceptInfo }), Dump(dependencies));
        }

        private static string Dump(IEnumerable<IConceptInfo> list)
        {
            var result = "(" + string.Join(",", list.Select(Dump).OrderBy(s => s)) + ")";
            Console.WriteLine(result);
            return result;
        }

        private static string Dump(IConceptInfo ci)
        {
            var result = ci.GetKey();
            Console.WriteLine(result);
            return result;
        }

        //=========================================================================

        [TestMethod]
        public void GetErrorDescription()
        {
            var simpleConceptInfo = new SimpleConceptInfo { Name = "s", Data = "d" };
            var refConceptInfo = new RefConceptInfo { Name = "r", Reference = simpleConceptInfo };
            var refRefConceptInfo = new RefRefConceptInfo { Name = "rr", Reference = refConceptInfo };

            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+RefRefConceptInfo Name=rr Reference=r.s", refRefConceptInfo.GetErrorDescription());

            refRefConceptInfo.Name = null;
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+RefRefConceptInfo Name=<null> Reference=r.s", refRefConceptInfo.GetErrorDescription());
            refRefConceptInfo.Name = "rr";

            refRefConceptInfo.Reference = null;
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+RefRefConceptInfo Name=rr Reference=<null>", refRefConceptInfo.GetErrorDescription());
            refRefConceptInfo.Reference = refConceptInfo;

            simpleConceptInfo.Name = null;
            TestUtility.AssertContains(refRefConceptInfo.GetErrorDescription(),
                new[] { refRefConceptInfo.GetType().FullName, "Name=rr", "Reference=", "null" });
            simpleConceptInfo.Name = "s";

            Assert.AreEqual("<null>", ConceptInfoHelper.GetErrorDescription(null));

            Assert.AreEqual(typeof(SimpleConceptInfo).FullName + " Name=s Data=<null>", new SimpleConceptInfo { Name = "s", Data = null }.GetErrorDescription());
        }


        private static RefConceptInfo GetSampleConcept()
        {
            return new RefConceptInfo
            {
                Name = "Test",
                Reference = new SimpleConceptInfo
                {
                    Name = "Test",
                    Data = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum"
                }
            };
        }

        [TestMethod]
        public void PerformanceTest()
        {
            int loopCount = 1000;
            var concepts = new List<RefConceptInfo>();
            for (int i = 0; i < loopCount; i++)
            {
                concepts.Add(GetSampleConcept());
            }
            //new SimpleConceptInfo { Name = null, Data = "d" }.GetKey();

            /*List<Func<IConceptInfo, bool, string, string>> functions = new List<Func<IConceptInfo, bool, string, string>>();
            for (var i = 0; i < 100*loopCount; i++)
            {
                functions.Add(ConceptInfoHelper.CreateCompiledGetSubKey(typeof(RefConceptInfo)));
            }*/
            var compiledGetKey = ConceptInfoHelper.CreateSerializeMembersFunction(typeof(RefConceptInfo));
            for (int i = 0; i < 10; i++)
            {
                var cocept = GetSampleConcept();
                var key = compiledGetKey(cocept, ConceptInfoHelper.SerializationOptions.KeyMembers, true);
            }
            for (int i = 0; i < 10; i++)
            {
                var cocept = GetSampleConcept();
                var key = "RefConceptInfo " + ConceptInfoHelper.SafeDelimit(cocept.Name) + "." + ConceptInfoHelper.SafeDelimit(cocept.Reference.Name) + "." + ConceptInfoHelper.SafeDelimit(cocept.Reference.Data);
            }
            for (int i = 0; i < 10; i++)
            {
                var concept = GetSampleConcept();
                var key = GetKeyFromStringBuilder(concept);
            }
            for (int i = 0; i < 10; i++)
            {
                var concept = GetSampleConcept();
                var key = GetKeyFromStringJoin(concept);
            }
            for (int i = 0; i < loopCount; i++)
            {
                var concept = GetSampleConcept();
                var key = ConceptInfoHelper.CreateKeyOld(concept);
            }

            var sw5 = new Stopwatch();
            sw5.Start();
            for (int i = 0; i < loopCount; i++)
            {
                var concept = concepts[i];
                var key = ConceptInfoHelper.CreateKeyOld(concept);
            }
            sw5.Stop();

            var sw1 = new Stopwatch();
            sw1.Start();
            for (int i = 0; i < loopCount; i++)
            {
                var concept = concepts[i];
                var key = compiledGetKey(concept, ConceptInfoHelper.SerializationOptions.KeyMembers, true); ;
            }
            sw1.Stop();

            var sw2 = new Stopwatch();
            sw2.Start();
            for (int i = 0; i < loopCount; i++)
            {
                var concept = concepts[i];
                if (concept.Name == null)
                    throw new Exception();
                if (concept.Reference.Name == null)
                    throw new Exception();
                var key = "RefConceptInfo " + ConceptInfoHelper.SafeDelimit(concept.Name) + "." + ConceptInfoHelper.SafeDelimit(concept.Reference.Name);
            }
            sw2.Stop();

            var sw3 = new Stopwatch();
            sw3.Start();
            for (int i = 0; i < loopCount; i++)
            {
                var concept = concepts[i];
                var key = GetKeyFromStringBuilder(concept);
            }
            sw3.Stop();

            var sw4 = new Stopwatch();
            sw4.Start();
            for (int i = 0; i < loopCount; i++)
            {
                var concept = concepts[i];
                var key = GetKeyFromStringJoin(concept);
            }
            sw4.Stop();

            var sw6 = new Stopwatch();
            sw6.Start();
            for (int i = 0; i < loopCount; i++)
            {
                var concept = concepts[i];
                var key = JsonConvert.SerializeObject(concept);
            }
            sw6.Stop();
        }

        private static string GetKeyFromStringBuilder(RefConceptInfo concept)
        {
            var sb = new StringBuilder();
            sb.Append("RefConceptInfo ");
            sb.Append(ConceptInfoHelper.SafeDelimit(concept.Name));
            sb.Append(".");
            sb.Append(ConceptInfoHelper.SafeDelimit(concept.Reference.Name));
            //sb.Append(".");
            //sb.Append(ConceptInfoHelper.SafeDelimit(concept.Reference.Data));
            return sb.ToString();
        }

        private static string GetKeyFromStringJoin(RefConceptInfo concept)
        {
            return string.Join("RefConceptInfo ", ConceptInfoHelper.SafeDelimit(concept.Name), ".", ConceptInfoHelper.SafeDelimit(concept.Reference.Name));
        }

        public static void AppendMemeberExpression(Expression memberExpression, Type type, ref Expression currentExpression, ref bool firstMember)
        {
            foreach (var conceptMember in ConceptMembers.Get(type).Where(x => x.IsKey))
            {
                if (conceptMember.IsConceptInfo)
                {
                    var returnType = (conceptMember.MemberInfo as PropertyInfo).PropertyType;
                    AppendMemeberExpression(
                        Expression.PropertyOrField(
                            Expression.Convert(memberExpression, type),
                            conceptMember.MemberInfo.Name
                            ),
                        returnType,
                        ref currentExpression,
                        ref firstMember
                        );
                }
                else if (firstMember)
                {
                    firstMember = false;
                    currentExpression = Expression.Add(
                        Expression.Add(currentExpression, Expression.Constant(" "), typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })),
                        Expression.PropertyOrField(
                            Expression.Convert(memberExpression, type),
                            conceptMember.MemberInfo.Name
                            ),
                        typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                }
                else
                {
                    currentExpression = Expression.Add(
                        Expression.Add(currentExpression, Expression.Constant("."), typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })),
                        Expression.PropertyOrField(
                            Expression.Convert(memberExpression, type),
                            conceptMember.MemberInfo.Name
                            ),
                        typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                }
            }
        }

        [TestMethod]
        public void CompileAtRuntimeTest()
        {
            var parameterExpr = Expression.Parameter(typeof(IConceptInfo), "x");
            /*BinaryExpression calculationExpresion = null;
            foreach (var conceptMember in ConceptMembers.Get(typeof(SimpleConceptInfo)))
            {
                if (calculationExpresion == null)
                {
                    calculationExpresion = Expression.Add(
                        Expression.Constant(""),
                        Expression.PropertyOrField(
                            Expression.Convert(parameterExpr, conceptMember.MemberInfo.DeclaringType),
                            conceptMember.MemberInfo.Name
                            ),
                      typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                }
                else {
                    calculationExpresion = Expression.Add(
                        calculationExpresion,
                        Expression.PropertyOrField(
                            Expression.Convert(parameterExpr, conceptMember.MemberInfo.DeclaringType),
                            conceptMember.MemberInfo.Name
                            ),
                      typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                }
            }*/

            /*var appendMemeberExpresion = Expression.Constant("") as Expression;
            AppendMemeberExpression(parameterExpr, typeof(SimpleConceptInfo), ref appendMemeberExpresion);
            var finalExpression = Expression.Lambda<Func<IConceptInfo, string>>(appendMemeberExpresion, parameterExpr);
            Func<IConceptInfo, string> getKeyFunc = finalExpression.Compile();*/
        }
    }
}