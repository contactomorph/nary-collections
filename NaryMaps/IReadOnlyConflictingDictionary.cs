namespace NaryMaps;

public interface IReadOnlyConflictingDictionary<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    bool IsConflictingWith(TValue value);

    List<TValue> GetConflictingItemsWith(TValue value);
    
    bool Contains(TValue value);
}