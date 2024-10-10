namespace NaryMaps;

public interface IRemoveOnlyMultiDictionary<TKey, TValue> : IReadOnlyMultiDictionary<TKey, TValue>
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    bool RemoveKey(TKey key);

    void Clear();

    public new IRemoveOnlyDictionary<TKey, IEnumerable<TValue>> AsDictionary { get; }
}