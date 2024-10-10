namespace NaryMaps;

public interface IReadOnlyMultiDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    public IEnumerable<TKey> Keys { get; }
    public IEnumerable<TValue> Values { get; }
    public IEnumerable<TValue> this[TKey key] { get; }
    public bool ContainsKey(TKey key);
    public IReadOnlyDictionary<TKey, IEnumerable<TValue>> AsDictionary { get; }
}