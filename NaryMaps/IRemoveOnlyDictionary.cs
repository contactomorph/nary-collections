namespace NaryMaps;

public interface IRemoveOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    bool RemoveKey(TKey key);

    void Clear();
}