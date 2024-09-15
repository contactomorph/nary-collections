using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class UniqueSearchableSelection<TDataTuple, TDataEntry, TComparerTuple, THandler, TSchema, T> :
    SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>,
    ISelection<TSchema, CompositeKind.UniqueSearchable, T>,
    IReadOnlyDictionary<T, TDataTuple>,
    IReadOnlyDictionary<T, IEnumerable<TDataTuple>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where THandler : struct, IHashTableProvider, IDataEquator<TDataEntry, TComparerTuple, T>
    where TSchema : Schema<TDataTuple>, new()
#if !NET6_0_OR_GREATER
    where T : notnull
#endif
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected UniqueSearchableSelection(NaryMapCore<TDataEntry, TComparerTuple> map) : base(map) { }

    #region Implement IReadOnlyDictionary<T, IEnumerable<TDataTuple>>
    
    IEnumerator<KeyValuePair<T, IEnumerable<TDataTuple>>>
        IEnumerable<KeyValuePair<T, IEnumerable<TDataTuple>>>.GetEnumerator()
    {
        return this
            .Select((KeyValuePair<T, TDataTuple> p) => new KeyValuePair<T, IEnumerable<TDataTuple>>(p.Key, [p.Value]))
            .GetEnumerator();
    }

    public IEnumerable<T> Keys
    {
        get
        {
            var handler = GetHandler();
            HashEntry[] hashTable = handler.GetHashTable();
            uint expectedVersion = _map._version;
            var dataTable = _map._dataTable;
            foreach (var entry in hashTable)
            {
                if (entry.DriftPlusOne == HashEntry.DriftForUnused)
                    continue;
                yield return GetItem(dataTable[entry.ForwardIndex]);

                if (expectedVersion != _map._version)
                    throw new InvalidOperationException("The map was modified after the enumerator was created.");
            }
        }
    }

    IEnumerable<IEnumerable<TDataTuple>> IReadOnlyDictionary<T, IEnumerable<TDataTuple>>.Values
    {
        get
        {
            IReadOnlyDictionary<T, TDataTuple> that = this;
            foreach (var pair in that)
                yield return [pair.Value];
        }
    }

    public bool ContainsKey(T key)
    {
        THandler handler = GetHandler();
        HashEntry[] hashTable = handler.GetHashTable();

        uint hc = GetHashCodeUsing(_map._comparerTuple, key);
        var result = MembershipHandling<TDataEntry, TComparerTuple, T, THandler>.Find(
            hashTable,
            _map._dataTable,
            handler,
            _map._comparerTuple,
            hc,
            key);
        return result.Case == SearchCase.ItemFound;
    }

    IEnumerable<TDataTuple> IReadOnlyDictionary<T, IEnumerable<TDataTuple>>.this[T key]
    {
        get
        {
            if (TryGetValue(key, out IEnumerable<TDataTuple> values))
                return values;
            throw new KeyNotFoundException();
        }
    }

    public bool TryGetValue(T key, out IEnumerable<TDataTuple> value)
    {
        if (TryGetValue(key, out TDataTuple dataTuple))
        {
            value = [dataTuple];
            return true;
        }
        value = null!;
        return false;
    }

    #endregion

    #region Implement IReadOnlyDictionary<T, TDataTuple>

    public IEnumerator<KeyValuePair<T, TDataTuple>> GetEnumerator()
    {
        uint expectedVersion = _map._version;
        var dataTable = _map._dataTable;
        
        for (int i = 0; i < _map._count; i++)
        {
            T item = GetItem(dataTable[i]);
            TDataTuple dataTuple = GetDataTuple(dataTable[i]);
            yield return new(item, dataTuple);
            if (expectedVersion != _map._version)
                throw new InvalidOperationException("The map was modified after the enumerator was created.");
        }
    }

    public IEnumerable<TDataTuple> Values
    {
        get
        {
            IReadOnlyDictionary<T, TDataTuple> that = this;
            foreach (var pair in that)
                yield return pair.Value;
        }
    }

    public TDataTuple this[T key]
    {
        get
        {
            if (TryGetValue(key, out TDataTuple values))
                return values;
            throw new KeyNotFoundException();
        }
    }
    
    public bool TryGetValue(T key, out TDataTuple value)
    {
        THandler handler = GetHandler();
        HashEntry[] hashTable = handler.GetHashTable();
        
        uint hc = GetHashCodeUsing(_map._comparerTuple, key);
        var result = MembershipHandling<TDataEntry, TComparerTuple, T, THandler>.Find(
            hashTable,
            _map._dataTable,
            handler,
            _map._comparerTuple,
            hc,
            key);
        
        if (result.Case == SearchCase.ItemFound)
        {
            value = GetDataTuple(_map._dataTable[result.ForwardIndex]);
            return true;
        }

        value = default;
        return false;
    }

    #endregion
    
    public sealed override IEnumerator<T> GetKeyEnumerator() => Keys.GetEnumerator();
    public sealed override IEnumerator GetPairEnumerator()
    {
        IReadOnlyDictionary<T, TDataTuple> that = this;
        return that.GetEnumerator();
    }
    public sealed override int GetKeyCount() => _map._count;
    public sealed override bool ContainsAsKey(T item) => ContainsKey(item);
}