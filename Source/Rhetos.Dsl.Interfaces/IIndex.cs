namespace Rhetos.Dsl
{
    public interface IIndex<in TKey, TValue>
    {
        TValue this[TKey key] { get; }

        bool TryGetValue(TKey key, out TValue value);
    }
}