using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ProxyDictionaryOfEnumerable<TKey, TDataTuple> : IRemoveOnlyDictionary<TKey, IEnumerable<TDataTuple>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;
    private readonly IConflictingSet<TDataTuple> _map;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyDictionaryOfEnumerable(SelectionBase<TDataTuple, TKey> selection)
    {
        _selection = selection;
        _map = selection.GetMapAsSet();
    }

    #region Implements IReadOnlyCollection<KeyValuePair<TKey,IEnumerable<TDataTuple>>>

    public int Count => _selection.GetKeyCount();
    
    public IEnumerator<KeyValuePair<TKey, IEnumerable<TDataTuple>>> GetEnumerator()
    {
        return _selection.GetItemAndDataTuplesEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    #endregion
    
    #region Implements IReadOnlyDictionary<TKey,IEnumerable<TDataTuple>>
    
    public bool ContainsKey(TKey key) => _selection.ContainsItem(key);

    public bool TryGetValue(TKey key, out IEnumerable<TDataTuple> value)
    {
        var dataTuples = _selection.GetDataTuplesFor(key);
        if (dataTuples is not null)
        {
            value = dataTuples;
            return true;
        }
        value = default!;
        return false;
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

    public IEnumerable<TKey> Keys => _selection.GetItemEnumerable();

    public IEnumerable<IEnumerable<TDataTuple>> Values
    {
        get
        {
            foreach (var (_, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
                yield return dataTuples;
        }
    }
    
    #endregion
    
    #region Implements IRemoveOnlyDictionary<TKey, IEnumerable<TDataTuple>>
    
    public bool RemoveKey(TKey key) => _selection.RemoveAllAt(key);
    
    public void Clear() => _map.Clear();
    
    #endregion
}

public sealed class ProxyDictionaryOfEnumerable<TKey, TValue, TDataTuple> :
    IRemoveOnlyDictionary<TKey, IEnumerable<TValue>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;
    private readonly IConflictingSet<TDataTuple> _map;
    private readonly Func<TDataTuple, TValue> _selector;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyDictionaryOfEnumerable(SelectionBase<TDataTuple, TKey> selection, Func<TDataTuple, TValue> selector)
    {
        _selection = selection;
        _map = selection.GetMapAsSet();
        _selector = selector;
    }

    #region Implements IReadOnlyCollection<KeyValuePair<TKey,IEnumerable<TDataTuple>>>
    
    public IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> GetEnumerator()
    {
        return _selection
            .GetItemAndDataTuplesEnumerable()
            .Select(p => KeyValuePair.Create(p.Key, p.Value.Select(_selector)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _selection.GetKeyCount();
    
    #endregion
    
    #region Implements IReadOnlyDictionary<TKey,IEnumerable<TDataTuple>>
    
    public bool ContainsKey(TKey key) => _selection.ContainsItem(key);

    public bool TryGetValue(TKey key, out IEnumerable<TValue> value)
    {
        var dataTuples = _selection.GetDataTuplesFor(key);
        if (dataTuples is not null)
        {
            value = dataTuples.Select(_selector);
            return true;
        }
        value = default!;
        return false;
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

    public IEnumerable<TKey> Keys => _selection.GetItemEnumerable();

    public IEnumerable<IEnumerable<TValue>> Values
    {
        get
        {
            foreach (var (_, dataTuples) in _selection.GetItemAndDataTuplesEnumerable())
                yield return dataTuples.Select(_selector);
        }
    }
    
    #endregion
    
    #region Implements IRemoveOnlyDictionary<TKey, IEnumerable<TDataTuple>>
    
    public bool RemoveKey(TKey key) => _selection.RemoveAllAt(key);
    
    public void Clear() => _map.Clear();
    
    #endregion
}