using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Compiler
{
    public class CsMarker
    {
        public static string GenerateMarker<T>(T conceptInfo, Expression<Func<T, string>> codeSnippetProperty) where T : IConceptInfo
        {
            var memberExpression = codeSnippetProperty.Body as MemberExpression;
            if (memberExpression == null)
                throw new FrameworkException("Invalid GenerateMarker method argument: codeSnippetProperty. The argument should be a lambda expression selecting a property of the class "
                    + typeof(T).Name + ". For example: \"conceptInfo => conceptInfo.Code\".");

            var property = memberExpression.Member as PropertyInfo;
            if (property == null || memberExpression.Expression.NodeType != ExpressionType.Parameter)
                throw new FrameworkException("Invalid GenerateMarker method argument: codeSnippetProperty. The argument should be a lambda expression selecting a property of the class "
                    + typeof(T).Name + ". For example: \"conceptInfo => conceptInfo.Code\".");

            var codeSnippet = property.GetValue(conceptInfo) as string;

            return $@"/*Start marker {conceptInfo.GetKey()} {property.Name}*/{codeSnippet}/*End marker {conceptInfo.GetKey()} {property.Name}*/";
        }
    }
}
