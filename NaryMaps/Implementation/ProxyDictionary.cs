using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ProxyDictionary<TKey, TDataTuple>(SelectionBase<TDataTuple, TKey> selection) :
    IReadOnlyDictionary<TKey, TDataTuple>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    public IEnumerator<KeyValuePair<TKey, TDataTuple>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
            yield return KeyValuePair.Create(key, dataTuples.First());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => selection.GetDataTupleCount();
    public bool ContainsKey(TKey key) => selection.ContainsItem(key);

    public bool TryGetValue(TKey key, out TDataTuple value)
    {
        var dataTuple = selection.GetFirstDataTupleFor(key);
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
            var dataTuple = selection.GetFirstDataTupleFor(key);
            if (dataTuple.HasValue)
                return dataTuple.Value;
            throw new KeyNotFoundException();
        }
    }

    public IEnumerable<TKey> Keys => selection.GetItemEnumerable();
    public IEnumerable<TDataTuple> Values
    {
        get
        {
            foreach (var (_, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
            foreach (var dataTuple in dataTuples)
                yield return dataTuple;
        }
    }
}

public sealed class ProxyDictionary<TKey, TValue, TDataTuple>(
    SelectionBase<TDataTuple, TKey> selection,
    Func<TDataTuple, TValue> selector) : IReadOnlyDictionary<TKey, TValue>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
            yield return KeyValuePair.Create(key, selector(dataTuples.First()));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => selection.GetDataTupleCount();
    public bool ContainsKey(TKey key) => selection.ContainsItem(key);

    public bool TryGetValue(TKey key, out TValue value)
    {
        var dataTuple = selection.GetFirstDataTupleFor(key);
        if (dataTuple.HasValue)
        {
            value = selector(dataTuple.Value);
            return true;
        }
        value = default!;
        return false;
    }

    public TValue this[TKey key]
    {
        get
        {
            var dataTuple = selection.GetFirstDataTupleFor(key);
            if (dataTuple.HasValue)
                return selector(dataTuple.Value);
            throw new KeyNotFoundException();
        }
    }

    public IEnumerable<TKey> Keys => selection.GetItemEnumerable();
    public IEnumerable<TValue> Values
    {
        get
        {
            foreach (var (_, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
            foreach (var dataTuple in dataTuples)
                yield return selector(dataTuple);
        }
    }
}