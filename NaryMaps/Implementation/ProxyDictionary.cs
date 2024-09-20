using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ProxyDictionary<TKey, TDataTuple> : IConflictingDictionary<TKey, TDataTuple>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;
    private readonly IConflictingSet<TDataTuple> _map;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyDictionary(SelectionBase<TDataTuple, TKey> selection)
    {
        _selection = selection;
        _map = selection.GetMapAsSet();
    }

    #region Implements IReadOnlyCollection<KeyValuePair<TKey,TDataTuple>>
    
    public int Count => _selection.GetDataTupleCount();
    
    public IEnumerator<KeyValuePair<TKey, TDataTuple>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
            yield return KeyValuePair.Create(key, dataTuples.First());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    #endregion
    
    #region Implements IReadOnlyDictionary<TKey,TDataTuple>

    public bool ContainsKey(TKey key) => _selection.ContainsItem(key);
    
    public bool TryGetValue(TKey key, out TDataTuple value)
    {
        var dataTuple = _selection.GetFirstDataTupleFor(key);
        if (dataTuple.HasValue)
        {
            value = dataTuple.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TDataTuple this[TKey key]
    {
        get
        {
            var dataTuple = _selection.GetFirstDataTupleFor(key);
            if (dataTuple.HasValue) return dataTuple.Value;
            throw new KeyNotFoundException();
        }
    }

    public IEnumerable<TKey> Keys => _selection.GetItemEnumerable();

    public IEnumerable<TDataTuple> Values
    {
        get
        {
            foreach (var (_, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
            foreach (var dataTuple in dataTuples)
                yield return dataTuple;
        }
    }
    
    #endregion

    #region Implements IReadOnlyConflictingDictionary<TKey,TDataTuple>

    public bool IsConflictingWith(TDataTuple item) => _map.IsConflictingWith(item);
    
    public List<TDataTuple> GetConflictingItemsWith(TDataTuple item) => _map.GetConflictingItemsWith(item);
    
    public bool Contains(TDataTuple item) => _map.Contains(item);

    #endregion
    
    #region Implements IRemoveOnlyDictionary<TKey, TValue>
    
    public bool RemoveKey(TKey key) => _selection.RemoveAllAt(key);
    
    public void Clear() => _map.Clear();

    #endregion
    
    #region Implements IConflictingDictionary<TKey,TDataTuple>
    
    public bool AddIfNoConflictFound(TDataTuple dataTuple) => _map.Add(dataTuple);

    public bool ForceAdd(TDataTuple dataTuple) => _map.ForceAdd(dataTuple);

    public bool Remove(TDataTuple dataTuple) => _map.Remove(dataTuple);

    #endregion
}

public sealed class ProxyDictionary<TKey, TValue, TDataTuple> : IRemoveOnlyDictionary<TKey, TValue>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;
    private readonly IConflictingSet<TDataTuple> _map;
    private readonly Func<TDataTuple, TValue> _selector;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyDictionary(SelectionBase<TDataTuple, TKey> selection, Func<TDataTuple, TValue> selector)
    {
        _selection = selection;
        _map = selection.GetMapAsSet();
        _selector = selector;
    }

    #region Implements IReadOnlyCollection<KeyValuePair<TKey,TValue>>

    public int Count => _selection.GetDataTupleCount();
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
            yield return KeyValuePair.Create(key, _selector(dataTuples.First()));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    #endregion
    
    #region Implements IReadOnlyDictionary<TKey,TValue>
    
    public bool ContainsKey(TKey key) => _selection.ContainsItem(key);

    public bool TryGetValue(TKey key, out TValue value)
    {
        var dataTuple = _selection.GetFirstDataTupleFor(key);
        if (dataTuple.HasValue)
        {
            value = _selector(dataTuple.Value);
            return true;
        }
        value = default!;
        return false;
    }

    public TValue this[TKey key]
    {
        get
        {
            var dataTuple = _selection.GetFirstDataTupleFor(key);
            if (dataTuple.HasValue)
                return _selector(dataTuple.Value);
            throw new KeyNotFoundException();
        }
    }

    public IEnumerable<TKey> Keys => _selection.GetItemEnumerable();
    public IEnumerable<TValue> Values
    {
        get
        {
            foreach (var (_, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
            foreach (var dataTuple in dataTuples)
                yield return _selector(dataTuple);
        }
    }
    
    #endregion
    
    #region Implements IRemoveOnlyDictionary<TKey, TValue>
    
    public bool RemoveKey(TKey key) => _selection.RemoveAllAt(key);
    
    public void Clear() => _map.Clear();
    
    #endregion
}