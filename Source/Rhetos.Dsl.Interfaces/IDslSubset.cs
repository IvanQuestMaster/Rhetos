using System;
using System.Collections.Generic;

namespace Rhetos.Dsl
{
    public interface IDslSubset<out TSource> : IEnumerable<TSource> where TSource : IConceptInfo
    {
        IDslSubset<TDestination> Query<TDestination>(Func<IEnumerable<TSource>, IEnumerable<TDestination>> query) where TDestination : IConceptInfo;
    }
}
