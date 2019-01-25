using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl
{
    public class DslTracker : IDslModel, IDslTracker
    {
        private DslContainer _dslContainer;

        private HashSet<IConceptInfo> _serachedConcepts;

        public IEnumerable<IConceptInfo> SerachedConcepts {
            get { return _serachedConcepts; }
        }

        public IDslSubset<IConceptInfo> Concepts
        {
            get
            {
                _serachedConcepts.UnionWith(_dslContainer.Concepts);
                return _dslContainer.Concepts;
            }
        }

        public DslTracker(DslContainer dslContainer)
        {
            _dslContainer = dslContainer;
            _serachedConcepts = new HashSet<IConceptInfo>();
        }

        public void Reset()
        {
            _serachedConcepts.Clear();
        }

        public IConceptInfo FindByKey(string conceptKey)
        {
            var conceptByKey = _dslContainer.FindByKey(conceptKey);
            _serachedConcepts.Add(conceptByKey);
            return conceptByKey;
        }

        public IDslSubset<TResult> QueryIndex<TIndex, TResult>(Func<TIndex, IEnumerable<TResult>> query)
            where TIndex : IDslModelIndex
            where TResult : IConceptInfo
        {
            return new DslSubset<TResult>(this, _dslContainer.QueryIndex<TIndex, TResult>(query));
        }

        public void AddConceptToTracker(IConceptInfo conceptInfo)
        {
            _serachedConcepts.Add(conceptInfo);
        }
    }
}
