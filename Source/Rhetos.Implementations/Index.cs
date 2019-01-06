namespace Rhetos
{
    public class Index<TKey, TValue> : Rhetos.IIndex<TKey, TValue>
    {
        Autofac.Features.Indexed.IIndex<TKey, TValue> _index;

        public TValue this[TKey key] {
            get { return _index[key]; }
        }

        public Index(Autofac.Features.Indexed.IIndex<TKey, TValue> index)
        {
            _index = index;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _index.TryGetValue(key, out value);
        }
    }
}
