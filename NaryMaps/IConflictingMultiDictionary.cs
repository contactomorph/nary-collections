namespace NaryMaps;

public interface IConflictingMultiDictionary<TKey, TValue> :
    IRemoveOnlyMultiDictionary<TKey, TValue>,
    IReadOnlyConflictingMultiDictionary<TKey, TValue>
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    bool AddIfNoConflictFound(TValue value);
    
    bool ForceAdd(TValue value);
    
    bool Remove(TValue value);
}