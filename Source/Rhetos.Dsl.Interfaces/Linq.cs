using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl
{
    public static class Linq
    {
        public static IDslSubset<TSource> Where<TSource>(this IDslSubset<TSource> source, Func<TSource, int, bool> predicate) where TSource : IConceptInfo
        {
            return source.Query<TSource>(x => x.Where(predicate));
        }

        public static IDslSubset<TSource> Where<TSource>(this IDslSubset<TSource> source, Func<TSource, bool> predicate) where TSource : IConceptInfo
        {
            return source.Query<TSource>(x => x.Where(predicate));
        }

        public static IDslSubset<TResult> OfType<TResult>(this IDslSubset<IConceptInfo> source) where TResult : IConceptInfo
        {
            return source.Query<TResult>(x => x.OfType<TResult>());
        }
    }
}
