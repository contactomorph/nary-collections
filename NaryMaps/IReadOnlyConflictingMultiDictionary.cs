namespace NaryMaps;

public interface IReadOnlyConflictingMultiDictionary<TKey, TValue> : IReadOnlyMultiDictionary<TKey, TValue>
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    bool IsConflictingWith(TValue value);

    List<TValue> GetConflictingItemsWith(TValue value);
    
    bool Contains(TValue value);
}