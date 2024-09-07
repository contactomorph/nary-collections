using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class UniqueSearchableSelection<TDataTuple, TDataEntry, TComparerTuple, THandler, TSchema, T> :
    SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>,
    ISelection<TSchema, CompositeKind.UniqueSearchable, T>,
    IReadOnlySet<T>,
    IReadOnlyDictionary<T, TDataTuple>,
    IReadOnlyDictionary<T, IEnumerable<TDataTuple>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where THandler : struct, IHashTableProvider, IDataEquator<TDataEntry, TComparerTuple, T>
    where TSchema : Schema<TDataTuple>, new()
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected UniqueSearchableSelection(NaryMapCore<TDataEntry, TComparerTuple> map) : base(map) { }

    #region Implement IReadOnlyCollection<T>

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Keys.GetEnumerator();

    public int Count => _map._count;

    #endregion

    #region Implement IReadOnlySet<T>

    public bool Contains(T item) => ContainsKey(item);

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    #endregion

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
            foreach (var pair in this)
                yield return [pair.Value];
        }
    }

    public bool ContainsKey(T key)
    {
        THandler handler = GetHandler();
        HashEntry[] hashTable = handler.GetHashTable();

        uint hc = ComputeHashCode(_map._comparerTuple, key);
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
            foreach (var pair in this)
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
        
        uint hc = ComputeHashCode(_map._comparerTuple, key);
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
}