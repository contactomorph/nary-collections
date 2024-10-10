using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ProxyDictionaryOfEnumerable<TKey, TDataTuple> : IReadOnlyDictionary<TKey, IEnumerable<TDataTuple>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyDictionaryOfEnumerable(SelectionBase<TDataTuple, TKey> selection)
    {
        _selection = selection;
    }

    public IEnumerator<KeyValuePair<TKey, IEnumerable<TDataTuple>>> GetEnumerator()
    {
        return _selection.GetItemAndDataTuplesEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _selection.GetKeyCount();
    
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
}

public sealed class ProxyDictionaryOfEnumerable<TKey, TValue, TDataTuple> :
    IReadOnlyDictionary<TKey, IEnumerable<TValue>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;
    private readonly Func<TDataTuple, TValue> _selector;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyDictionaryOfEnumerable(SelectionBase<TDataTuple, TKey> selection,
        Func<TDataTuple, TValue> selector)
    {
        _selection = selection;
        _selector = selector;
    }

    public IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> GetEnumerator()
    {
        return _selection
            .GetItemAndDataTuplesEnumerable()
            .Select(p => KeyValuePair.Create(p.Key, p.Value.Select(_selector)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _selection.GetKeyCount();
    
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
}