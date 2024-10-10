namespace NaryMaps;

public interface IConflictingDictionary<TKey, TValue> : IRemoveOnlyDictionary<TKey, TValue>, IReadOnlyConflictingDictionary<TKey, TValue>
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    bool AddIfNoConflictFound(TValue value);
    
    bool ForceAdd(TValue value);
    
    bool Remove(TValue value);
}