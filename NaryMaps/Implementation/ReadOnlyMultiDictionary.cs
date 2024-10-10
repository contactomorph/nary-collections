using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ReadOnlyMultiDictionary<TKey, TDataTuple>(SelectionBase<TDataTuple, TKey> selection) :
    IReadOnlyMultiDictionary<TKey, TDataTuple>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    public IEnumerator<KeyValuePair<TKey, TDataTuple>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
        foreach (var dataTuple in dataTuples)
            yield return KeyValuePair.Create(key, dataTuple);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => selection.GetDataTupleCount();
    
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

    public bool ContainsKey(TKey key) => selection.ContainsItem(key);

    public IReadOnlyDictionary<TKey, IEnumerable<TDataTuple>> AsDictionary => throw new NotImplementedException();

    public bool TryGetValues(TKey key, out IEnumerable<TDataTuple> values)
    {
        var dataTuples = selection.GetDataTuplesFor(key);
        if (dataTuples is not null)
        {
            values = dataTuples;
            return true;
        }
        values = default!;
        return false;
    }
}

public sealed class ReadOnlyMultiDictionary<TKey, TValue, TDataTuple>(
    SelectionBase<TDataTuple, TKey> selection,
    Func<TDataTuple, TValue> selector) : IReadOnlyMultiDictionary<TKey, TValue>
    where TDataTuple : struct, ITuple, IStructuralEquatable
#if !NET6_0_OR_GREATER
    where TKey : notnull
#endif
{
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var (key, dataTuples) in selection.GetItemAndDataTuplesEnumerable())
        foreach (var dataTuple in dataTuples)
            yield return KeyValuePair.Create(key, selector(dataTuple));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => selection.GetDataTupleCount();
    
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

    public bool ContainsKey(TKey key) => selection.ContainsItem(key);

    public IReadOnlyDictionary<TKey, IEnumerable<TValue>> AsDictionary => throw new NotImplementedException();

    public bool TryGetValues(TKey key, out IEnumerable<TValue> values)
    {
        var dataTuples = selection.GetDataTuplesFor(key);
        if (dataTuples is not null)
        {
            values = dataTuples.Select(selector);
            return true;
        }
        values = default!;
        return false;
    }
}
