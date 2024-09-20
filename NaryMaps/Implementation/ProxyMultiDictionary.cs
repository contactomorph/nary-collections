using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ProxyMultiDictionary<TKey, TDataTuple> : IConflictingMultiDictionary<TKey, TDataTuple>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;
    private readonly IConflictingSet<TDataTuple> _map;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyMultiDictionary(SelectionBase<TDataTuple, TKey> selection)
    {
        _selection = selection;
        _map = selection.GetMapAsSet();
    }

    #region Implements IReadOnlyCollection<KeyValuePair<TKey,TDataTuple>>
    
    public IEnumerator<KeyValuePair<TKey, TDataTuple>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
        foreach (var dataTuple in dataTuples)
            yield return KeyValuePair.Create(key, dataTuple);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _selection.GetDataTupleCount();
    
    #endregion
    
    #region Implements IReadOnlyMultiDictionary<TKey, TValue>
    
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

    public IEnumerable<TDataTuple> this[TKey key]
    {
        get
        {
            var dataTuples = _selection.GetDataTuplesFor(key);
            if (dataTuples is not null)
                return dataTuples;
            throw new KeyNotFoundException();
        }
    }

    public bool ContainsKey(TKey key) => _selection.ContainsItem(key);

    IReadOnlyDictionary<TKey, IEnumerable<TDataTuple>> IReadOnlyMultiDictionary<TKey, TDataTuple>.AsDictionary =>
        AsDictionary;
    
    #endregion
    
    #region Implements IRemoveOnlyMultiDictionary<TKey, TValue>

    public IRemoveOnlyDictionary<TKey, IEnumerable<TDataTuple>> AsDictionary
    {
        get
        {
            // ReSharper disable once ArrangeAccessorOwnerBody
            return new ProxyDictionaryOfEnumerable<TKey, TDataTuple>(_selection);
        }
    }

    public bool RemoveKey(TKey key) => _selection.RemoveAllAt(key);
    
    public void Clear() => _map.Clear();
    
    #endregion

    #region Implements IReadOnlyConflictingMultiDictionary<TKey, TValue>
    
    public bool IsConflictingWith(TDataTuple item) => _map.IsConflictingWith(item);
    
    public List<TDataTuple> GetConflictingItemsWith(TDataTuple item) => _map.GetConflictingItemsWith(item);
    
    public bool Contains(TDataTuple item) => _map.Contains(item);

    #endregion

    #region Implements IConflictingMultiDictionary<TKey, TDataTuple>
    
    public bool AddIfNoConflictFound(TDataTuple dataTuple) => _map.Add(dataTuple);

    public bool ForceAdd(TDataTuple dataTuple) => _map.ForceAdd(dataTuple);

    public bool Remove(TDataTuple dataTuple) => _map.Remove(dataTuple);
    
    #endregion
}

public sealed class ProxyMultiDictionary<TKey, TValue, TDataTuple> : IRemoveOnlyMultiDictionary<TKey, TValue>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;
    private readonly IConflictingSet<TDataTuple> _map;
    private readonly Func<TDataTuple, TValue> _selector;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyMultiDictionary(SelectionBase<TDataTuple, TKey> selection, Func<TDataTuple, TValue> selector)
    {
        _selection = selection;
        _map = selection.GetMapAsSet();
        _selector = selector;
    }

    #region Implements IReadOnlyCollection<KeyValuePair<TKey,TDataTuple>>
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
        foreach (var dataTuple in dataTuples)
            yield return KeyValuePair.Create(key, _selector(dataTuple));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _selection.GetDataTupleCount();
    
    #endregion
    
    #region Implements IReadOnlyMultiDictionary<TKey, TValue>
    
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

    public IEnumerable<TValue> this[TKey key]
    {
        get
        {
            var dataTuples = _selection.GetDataTuplesFor(key);
            if (dataTuples is not null)
                return dataTuples.Select(_selector);
            throw new KeyNotFoundException();
        }
    }

    public bool ContainsKey(TKey key) => _selection.ContainsItem(key);

    IReadOnlyDictionary<TKey, IEnumerable<TValue>> IReadOnlyMultiDictionary<TKey, TValue>.AsDictionary => AsDictionary;
    
    #region Implements IRemoveOnlyMultiDictionary<TKey, TValue>

    public IRemoveOnlyDictionary<TKey, IEnumerable<TValue>> AsDictionary
    {
        get
        {
            // ReSharper disable once ArrangeAccessorOwnerBody
            return new ProxyDictionaryOfEnumerable<TKey, TValue, TDataTuple>(_selection, _selector);
        }
    }
    
    public bool RemoveKey(TKey key) => _selection.RemoveAllAt(key);
    
    public void Clear() => _map.Clear();

    #endregion
    
    #endregion
}
