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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rhetos.Dsl
{
    public static class ConceptInfoHelper
    {
        private static ConditionalWeakTable<IConceptInfo, string> KeyCache = new ConditionalWeakTable<IConceptInfo, string>();

        public static void AppendMemeberExpression(Expression memberExpression, Type type, List<Expression> validateExpression, ref Expression currentExpression, ref bool firstMember, bool useGetKeyProperties = false)
        {
            foreach (var conceptMember in ConceptMembers.Get(type).Where(x => x.IsKey))
            {
                if (conceptMember.IsConceptInfo)
                {
                    validateExpression.Add(Expression.IfThen(
                        Expression.Equal(Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name), Expression.Constant(null)),
                        Expression.Throw(Expression.Constant(new DslSyntaxException()))
                    ));

                    var returnType = (conceptMember.MemberInfo as PropertyInfo).PropertyType;
                    if (returnType == typeof(IConceptInfo))
                    {
                        if (firstMember)
                            firstMember = false;
                        else
                            currentExpression = Expression.Add(currentExpression, Expression.Constant("."), typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));

                        firstMember = false;
                        currentExpression = Expression.Add(
                            currentExpression,
                            Expression.Call(
                                typeof(ConceptInfoHelper).GetMethod("CreateSubKey"),
                                Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name),
                                Expression.Constant(":")
                                ),
                            typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                    }
                    else
                    {
                        AppendMemeberExpression(
                            Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name),
                            returnType,
                            validateExpression,
                            ref currentExpression,
                            ref firstMember
                            );
                    }
                }
                else
                {
                    validateExpression.Add(Expression.IfThen(
                        Expression.Equal(Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name), Expression.Constant(null)),
                        Expression.Throw(Expression.Constant(new DslSyntaxException())))
                        );

                    if (firstMember)
                        firstMember = false;
                    else
                        currentExpression = Expression.Add(currentExpression, Expression.Constant("."), typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));

                    firstMember = false;
                    currentExpression = Expression.Add(
                        currentExpression,
                        Expression.Call(
                            typeof(ConceptInfoHelper).GetMethod("SafeDelimit"),
                            Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name)
                            ),
                        typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
                }
            }
        }

        public static Func<IConceptInfo, bool, string, string> CreateCompiledGetSubKey(Type conceptType)
        {
            var conceptParamExpr = Expression.Parameter(typeof(IConceptInfo), "concept");
            var useTypeParamExpr = Expression.Parameter(typeof(bool), "useType");
            var typeSeparatorParamExpr = Expression.Parameter(typeof(string), "typeSeparator");
            var conceptExpression = Expression.Convert(conceptParamExpr, conceptType);
            var validateExpression = new List<Expression>();
            var appendMemeberExpresion = Expression.Condition(
                Expression.Equal(useTypeParamExpr, Expression.Constant(true)),
                    Expression.Add(Expression.Constant(ConceptInfoHelper.BaseConceptInfoType(conceptType).Name), typeSeparatorParamExpr,
                        typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })),
                    Expression.Constant("")
                ) as Expression;
            var firstMemeber = true;
            AppendMemeberExpression(conceptExpression, conceptType, validateExpression, ref appendMemeberExpresion, ref firstMemeber, true);
            var finalExpression = Expression.Lambda<Func<IConceptInfo, bool, string, string>>(Expression.Block(validateExpression.Union(new List<Expression> { appendMemeberExpresion })), conceptParamExpr, useTypeParamExpr, typeSeparatorParamExpr);
            return finalExpression.Compile();
        }

        private static Dictionary<Type, Func<IConceptInfo, bool, string, string>> _compiledGetSubKey = new Dictionary<Type, Func<IConceptInfo, bool, string, string>>();

        public static Func<IConceptInfo, bool, string, string> GetSubKeyFunction(Type type)
        {
            Func<IConceptInfo, bool, string, string> func;
            if (!_compiledGetSubKey.TryGetValue(type, out func))
            {
                func = CreateCompiledGetSubKey(type);
                _compiledGetSubKey.Add(type, func);
            }
            return  func;
        }

        public static string CreateSubKey(IConceptInfo ci, string typeSeparator)
        {
            Func<IConceptInfo, bool, string, string> func;
            if (!_compiledGetSubKey.TryGetValue(ci.GetType(), out func))
            {
                func = CreateCompiledGetSubKey(ci.GetType());
                _compiledGetSubKey.Add(ci.GetType(), func);
            }
            var key = func(ci, true, typeSeparator);
            return key;
        }

        public static string CreateKey(IConceptInfo ci)
        {
            return GetSubKeyFunction(ci.GetType())(ci, true, " ");
        }

        /// <summary>
        /// Returns a string that <b>uniquely describes the concept instance</b>.
        /// The string contains concept's base class type and a list of concept's key properties.
        /// </summary>
        /// <remarks>
        /// If the concept inherits another concept, the base class type will be used instead of
        /// actual concept's type to achieve normalized form. That way, the resulting string
        /// can be used in scenarios such as resolving references to other concepts where
        /// a reference can be of the base class type, but referencing inherited type.
        /// </remarks>
        public static string GetKey(this IConceptInfo ci)
        {
            if (ci == null)
                throw new ArgumentNullException();

            return KeyCache.GetValue(ci, CreateKey);
        }

        public static List<string> CreateKeyForConcepts = new List<string>();

        public static string CreateKeyOld(IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(100);
            desc.Append(BaseConceptInfoType(ci).Name);
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.KeyMembers, exceptionOnNullMember: true);
            CreateKeyForConcepts.Add(desc.ToString());
            return desc.ToString();
        }

        public static string GetShortDescription(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(100);
            desc.Append(ci.GetType().Name);
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.KeyMembers);
            return desc.ToString();
        }

        /// <summary>
        /// Returns a string that describes the concept instance in a user-friendly manner.
        /// The string contains concept's keyword and a list of concept's key properties.
        /// </summary>
        /// <remarks>
        /// This description in not unique because different concepts might have same keyword.
        /// </remarks>
        public static string GetUserDescription(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(100);
            desc.Append(GetKeywordOrTypeName(ci));
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.KeyMembers);
            return desc.ToString();
        }

        public static List<string> GetKeyPropertiesForConcepts = new List<string>();

        public static string GetKeyPropertiesOld(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(100);
            AppendMembers(desc, ci, SerializationOptions.KeyMembers, exceptionOnNullMember: true);
            GetKeyPropertiesForConcepts.Add(desc.ToString());
            return desc.ToString();
        }

        /// <summary>
        /// Returns a string with a dot-separated list of concept's key properties.
        /// </summary>
        public static string GetKeyProperties(this IConceptInfo ci)
        {
            return GetSubKeyFunction(ci.GetType())(ci, false, "");
        }

        /// <summary>
        /// Returns a string that fully describes the concept instance.
        /// The string contains concept's type name and all concept's properties.
        /// </summary>
        public static string GetFullDescription(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(200);
            desc.Append(ci.GetType().FullName);
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.AllMembers);
            return desc.ToString();
        }

        /// <summary>
        /// Returns a string that describes the concept instance cast as a base concept.
        /// The string contains base concept's type name and the base concept's properties.
        /// </summary>
        public static string GetFullDescriptionAsBaseConcept(this IConceptInfo ci, Type baseConceptType)
        {
            if (!baseConceptType.IsAssignableFrom(ci.GetType()))
                throw new FrameworkException($"{baseConceptType} is not assignable from {ci.GetUserDescription()}.");
            StringBuilder desc = new StringBuilder(200);
            desc.Append(baseConceptType.FullName);
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.AllMembers, false, baseConceptType);
            return desc.ToString();
        }

        /// <summary>
        /// Return value is null if IConceptInfo implementations does not have ConceptKeyword attribute. Such classes are usually used as a base class for other concepts.
        /// </summary>
        public static string GetKeyword(this IConceptInfo ci)
        {
            return GetKeyword(ci.GetType());
        }

        /// <summary>
        /// Return value is null if IConceptInfo implementations does not have ConceptKeyword attribute. Such classes are usually used as a base class for other concepts.
        /// </summary>
        public static string GetKeyword(Type conceptInfoType)
        {
            return conceptInfoType
                .GetCustomAttributes(typeof(ConceptKeywordAttribute), false)
                .Select(keywordAttribute => ((ConceptKeywordAttribute)keywordAttribute).Keyword)
                .SingleOrDefault();
        }

        public static string GetKeywordOrTypeName(this IConceptInfo ci)
        {
            return ci.GetKeyword() ?? ci.GetType().Name;
        }

        public static string GetKeywordOrTypeName(Type conceptInfoType)
        {
            return GetKeyword(conceptInfoType) ?? conceptInfoType.Name;
        }

        /// <summary>
        /// Returns a list of concepts that this concept directly depends on.
        /// </summary>
        public static IEnumerable<IConceptInfo> GetDirectDependencies(this IConceptInfo conceptInfo)
        {
            return (from member in ConceptMembers.Get(conceptInfo)
                    where member.IsConceptInfo
                    select (IConceptInfo)member.GetValue(conceptInfo)).Distinct().ToList();
        }

        /// <summary>
        /// Returns a list of concepts that this concept depends on directly or indirectly.
        /// </summary>
        public static IEnumerable<IConceptInfo> GetAllDependencies(this IConceptInfo conceptInfo)
        {
            var dependencies = new List<IConceptInfo>();
            AddAllDependencies(conceptInfo, dependencies);
            return dependencies;
        }

        /// <summary>
        /// Use only for generating an error details. Returns the concept's description ignoring possible null reference errors.
        /// </summary>
        public static string GetErrorDescription(this IConceptInfo ci)
        {
            if (ci == null)
                return "<null>";
            var report = new StringBuilder();
            report.Append(ci.GetType().FullName);
            foreach (var member in ConceptMembers.Get(ci))
            {
                report.Append(" " + member.Name + "=");
                var memberValue = member.GetValue(ci);
                try
                {
                    if (memberValue == null)
                        report.Append("<null>");
                    else if (member.IsConceptInfo)
                        AppendMembers(report, (IConceptInfo)memberValue, SerializationOptions.KeyMembers, exceptionOnNullMember: false);
                    else
                        report.Append(memberValue.ToString());
                }
                catch (Exception ex)
                {
                    report.Append("<" + ex.GetType().Name + ">");
                }
            }
            return report.ToString();
        }


        private static void AddAllDependencies(IConceptInfo conceptInfo, ICollection<IConceptInfo> dependencies)
        {
            foreach (var member in ConceptMembers.Get(conceptInfo))
                if (member.IsConceptInfo)
                {
                    var dependency = (IConceptInfo)member.GetValue(conceptInfo);
                    if (!dependencies.Contains(dependency))
                    {
                        dependencies.Add(dependency);
                        AddAllDependencies(dependency, dependencies);
                    }
                }
        }

        private enum SerializationOptions
        {
            KeyMembers,
            AllMembers
        };

        private static void AppendMembers(StringBuilder text, IConceptInfo ci, SerializationOptions serializationOptions, bool exceptionOnNullMember = false, Type asBaseConceptType = null)
        {
            IEnumerable<ConceptMember> members = asBaseConceptType != null ? ConceptMembers.Get(asBaseConceptType) : ConceptMembers.Get(ci);
            if (serializationOptions == SerializationOptions.KeyMembers)
                members = members.Where(member => member.IsKey);

            bool firstMember = true;
            foreach (ConceptMember member in members)
            {
                string separator = member.IsKey ? "." : " ";
                if (!firstMember)
                    text.Append(separator);
                firstMember = false;

                AppendMember(text, ci, member, exceptionOnNullMember);
            }
        }

        private static void AppendMember(StringBuilder text, IConceptInfo ci, ConceptMember member, bool exceptionOnNullMember)
        {
            object memberValue = member.GetValue(ci);
            if (memberValue == null)
                if (exceptionOnNullMember)
                    throw new DslSyntaxException(ci, string.Format(
                        "{0}'s property {1} is null. Info: {2}.",
                        ci.GetType().Name, member.Name, ci.GetErrorDescription()));
                else
                    text.Append("<null>");
            else if (member.IsConceptInfo)
            {
                IConceptInfo value = (IConceptInfo)member.GetValue(ci);
                if (member.ValueType == typeof(IConceptInfo))
                    text.Append(BaseConceptInfoType(value).Name).Append(":");
                AppendMembers(text, value, SerializationOptions.KeyMembers, exceptionOnNullMember);
            }
            else if (member.ValueType == typeof(string))
                text.Append(SafeDelimit(member.GetValue(ci).ToString()));
            else
                throw new FrameworkException(string.Format(
                    "IConceptInfo member {0} of type {1} in {2} is not supported.",
                    member.Name, member.ValueType.Name, ci.GetType().Name));
        }

        public static string SafeDelimit(string text)
        {
            bool clean = text.All(c => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_');
            if (clean && text.Length > 0)
                return text;
            string quote = (text.Contains('\'') && !text.Contains('\"')) ? "\"" : "\'";
            return quote + text.Replace(quote, quote + quote) + quote;
        }

        public static Type BaseConceptInfoType(this IConceptInfo ci)
        {
            Type t = ci.GetType();
            while (typeof(IConceptInfo).IsAssignableFrom(t.BaseType) && t.BaseType.IsClass)
                t = t.BaseType;
            return t;
        }

        public static Type BaseConceptInfoType(Type t)
        {
            while (typeof(IConceptInfo).IsAssignableFrom(t.BaseType) && t.BaseType.IsClass)
                t = t.BaseType;
            return t;
        }
    }
}