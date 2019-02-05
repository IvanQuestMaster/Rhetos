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

        public static string CreateKey(IConceptInfo ci)
        {
            return BaseConceptInfoType(ci).Name + " " + SerializeMembers(ci, SerializationOptions.KeyMembers, true);
            //return GetSubKeyFunction(ci.GetType())(ci, true, " ");
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
            return ci.GetType().Name + " " + SerializeMembers(ci, SerializationOptions.KeyMembers);
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
            return GetKeywordOrTypeName(ci) + " " + SerializeMembers(ci, SerializationOptions.KeyMembers); ;
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
            return SerializeMembers(ci, SerializationOptions.KeyMembers, exceptionOnNullMember: true);
        }

        /// <summary>
        /// Returns a string that fully describes the concept instance.
        /// The string contains concept's type name and all concept's properties.
        /// </summary>
        public static string GetFullDescription(this IConceptInfo ci)
        {
            return ci.GetType().FullName + " " + SerializeMembers(ci, SerializationOptions.AllMembers);
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

        public enum SerializationOptions
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

        public static string BaseConceptInfoTypeName(this IConceptInfo ci)
        {
            Type t = ci.GetType();
            while (typeof(IConceptInfo).IsAssignableFrom(t.BaseType) && t.BaseType.IsClass)
                t = t.BaseType;
            return t.Name;
        }

        public static DslSyntaxException ThrowDslSyntaxExceptionForConcept(IConceptInfo ci, string memeberName)
        {
            throw new DslSyntaxException(ci, string.Format(
                            "{0}'s property {1} is null. Info: {2}.",
                            ci.GetType().Name, memeberName, ci.GetErrorDescription())
                        );
        }

        private static Dictionary<Type, Func<IConceptInfo, SerializationOptions, bool, string>> _serializeMemebersCompiled = new Dictionary<Type, Func<IConceptInfo, SerializationOptions, bool, string>>();

        public static void GenerateSerializeMembersExpression(
                Expression memberExpression, Type type,
                List<Expression> validateMembersExpression,
                List<Expression> keyMembersEvaluationExpression,
                List<Expression> otherMembersEvaluationExpression,
                ParameterExpression serializationOptionsParameExpr,
                ParameterExpression exceptionOnNullMemberParamExpr,
                bool onlyKeyMembers,
                ref bool firstMember)
        {
            var memebers = onlyKeyMembers ? ConceptMembers.Get(type).Where(x => x.IsKey) : ConceptMembers.Get(type);
            foreach (var conceptMember in memebers)
            {
                var memeberExpressionList = conceptMember.IsKey ? keyMembersEvaluationExpression : otherMembersEvaluationExpression;

                if (conceptMember.IsKey)
                    validateMembersExpression.Add(Expression.IfThen(
                        Expression.Equal(Expression.PropertyOrField(memberExpression, conceptMember.Name), Expression.Constant(null)),
                        Expression.Call(typeof(ConceptInfoHelper).GetMethod("ThrowDslSyntaxExceptionForConcept"), memberExpression, Expression.Constant(conceptMember.Name))
                        ));

                if (conceptMember.IsConceptInfo)
                {
                    if (conceptMember.ValueType == typeof(IConceptInfo))
                    {
                        if (firstMember)
                            firstMember = false;
                        else
                            memeberExpressionList.Add(Expression.Constant("."));

                        memeberExpressionList.Add(Expression.Call(
                                typeof(ConceptInfoHelper).GetMethod("BaseConceptInfoTypeName", new[] { typeof(IConceptInfo) }),
                                Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name)
                                ));
                        memeberExpressionList.Add(Expression.Constant(":"));
                        memeberExpressionList.Add(
                            Expression.Condition(
                                Expression.Equal(Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name), Expression.Constant(null)),
                                Expression.Constant("<null>"),
                                Expression.Call(
                                    typeof(ConceptInfoHelper).GetMethod("SerializeMembers"),
                                    Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name),
                                    serializationOptionsParameExpr, exceptionOnNullMemberParamExpr
                                    )
                                )
                            );
                    }
                    else
                    {
                        GenerateSerializeMembersExpression(
                            Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name),
                            conceptMember.ValueType,
                            validateMembersExpression,
                            keyMembersEvaluationExpression,
                            otherMembersEvaluationExpression,
                            serializationOptionsParameExpr,
                            exceptionOnNullMemberParamExpr,
                            true,
                            ref firstMember
                            );
                    }
                }
                else if (conceptMember.ValueType == typeof(string))
                {
                    if (firstMember)
                        firstMember = false;
                    else
                        memeberExpressionList.Add(conceptMember.IsKey ? Expression.Constant(".") : Expression.Constant(" "));

                    memeberExpressionList.Add(
                        Expression.Condition(
                            Expression.Equal(Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name), Expression.Constant(null)),
                            Expression.Constant("<null>"),
                            Expression.Call(
                                typeof(ConceptInfoHelper).GetMethod("SafeDelimit"),
                                Expression.PropertyOrField(memberExpression, conceptMember.MemberInfo.Name)
                                )
                            )
                        );
                }
                else
                {
                    throw new FrameworkException(string.Format(
                        "IConceptInfo member {0} of type {1} in {2} is not supported.",
                        conceptMember.Name, conceptMember.ValueType.Name, type.Name));
                }
            }
        }

        public static Expression GenerateConcatenationExpression(List<Expression> stringEValuationExpressions)
        {
            Expression returnExpression = null;
            if (stringEValuationExpressions.Count == 2)
                returnExpression = Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }), stringEValuationExpressions[0], stringEValuationExpressions[1]);
            else if (stringEValuationExpressions.Count == 3)
                returnExpression = Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string), typeof(string) }), stringEValuationExpressions[0], stringEValuationExpressions[1], stringEValuationExpressions[2]);
            else if (stringEValuationExpressions.Count == 4)
                returnExpression = Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string), typeof(string), typeof(string) }), stringEValuationExpressions[0], stringEValuationExpressions[1], stringEValuationExpressions[2], stringEValuationExpressions[3]);
            else
                returnExpression = Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string[]) }), Expression.NewArrayInit(typeof(string), stringEValuationExpressions));
            return returnExpression;
        }

        public static Func<IConceptInfo, SerializationOptions, bool, string> CreateSerializeMembersFunction(Type conceptType)
        {
            var conceptParamExpr = Expression.Parameter(typeof(IConceptInfo), "concept");
            var serializationOptionsParameExpr = Expression.Parameter(typeof(SerializationOptions), "serializationOptions");
            var exceptionOnNullMemberParamExpr = Expression.Parameter(typeof(bool), "exceptionOnNullMember");
            var conceptExpression = Expression.Convert(conceptParamExpr, conceptType);

            var validateMembersExpression = new List<Expression>();
            var keyMembersEvaluationExpression = new List<Expression>();
            var otherMembersEvaluationExpression = new List<Expression>();

            var firstMemeber = true;
            GenerateSerializeMembersExpression(
                conceptExpression, conceptType,
                validateMembersExpression, keyMembersEvaluationExpression,
                otherMembersEvaluationExpression,
                serializationOptionsParameExpr,
                exceptionOnNullMemberParamExpr,
                false,
                ref firstMemeber);

            Expression returnExpression = Expression.Condition(
                Expression.Equal(serializationOptionsParameExpr, Expression.Constant(SerializationOptions.KeyMembers)),
                GenerateConcatenationExpression(keyMembersEvaluationExpression),
                GenerateConcatenationExpression(keyMembersEvaluationExpression.Union(otherMembersEvaluationExpression).ToList())
                );

            var finalExpression = Expression.Lambda<Func<IConceptInfo, SerializationOptions, bool, string>>(
                Expression.Block(validateMembersExpression.Union(new List<Expression> { returnExpression })),
                conceptParamExpr,
                serializationOptionsParameExpr,
                exceptionOnNullMemberParamExpr);
            return finalExpression.Compile();
        }

        public static string SerializeMembers(IConceptInfo ci, SerializationOptions serializationOptions, bool exceptionOnNullMember = false)
        {
            Func<IConceptInfo, SerializationOptions, bool, string> func = null;
            if (!_serializeMemebersCompiled.TryGetValue(ci.GetType(), out func))
            {
                func = CreateSerializeMembersFunction(ci.GetType());
            }
            return func(ci, serializationOptions, exceptionOnNullMember);
        }
    }
}