using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ReadOnlyDictionaryOfEnumerable<TKey, TDataTuple>(SelectionBase<TDataTuple, TKey> selection) :
    IReadOnlyDictionary<TKey, IEnumerable<TDataTuple>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    public IEnumerator<KeyValuePair<TKey, IEnumerable<TDataTuple>>> GetEnumerator()
    {
        return selection.GetItemAndDataTuplesEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => selection.GetKeyCount();
    
    public bool ContainsKey(TKey key) => selection.ContainsItem(key);

    public bool TryGetValue(TKey key, out IEnumerable<TDataTuple> value)
    {
        var dataTuples = selection.GetDataTuplesFor(key);
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
            var dataTuples = selection.GetDataTuplesFor(key);
            if (dataTuples is not null)
                return dataTuples;
            throw new KeyNotFoundException();
        }
    }

    public IEnumerable<TKey> Keys => selection.GetItemEnumerable();

    public IEnumerable<IEnumerable<TDataTuple>> Values
    {
        get
        {
            foreach (var (_, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
                yield return dataTuples;
        }
    }
}

public sealed class ReadOnlyDictionaryOfEnumerable<TKey, TValue, TDataTuple>(
    SelectionBase<TDataTuple, TKey> selection,
    Func<TDataTuple, TValue> selector) : IReadOnlyDictionary<TKey, IEnumerable<TValue>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    public IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> GetEnumerator()
    {
        return selection
            .GetItemAndDataTuplesEnumerable()
            .Select(p => KeyValuePair.Create(p.Key, p.Value.Select(selector)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => selection.GetKeyCount();
    
    public bool ContainsKey(TKey key) => selection.ContainsItem(key);

    public bool TryGetValue(TKey key, out IEnumerable<TValue> value)
    {
        var dataTuples = selection.GetDataTuplesFor(key);
        if (dataTuples is not null)
        {
            value = dataTuples.Select(selector);
            return true;
        }
        value = default!;
        return false;
    }

    public IEnumerable<TValue> this[TKey key]
    {
        get
        {
            var dataTuples = selection.GetDataTuplesFor(key);
            if (dataTuples is not null)
                return dataTuples.Select(selector);
            throw new KeyNotFoundException();
        }
    }

    public IEnumerable<TKey> Keys => selection.GetItemEnumerable();

    public IEnumerable<IEnumerable<TValue>> Values
    {
        get
        {
            foreach (var (_, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
                yield return dataTuples.Select(selector);
        }
    }
}