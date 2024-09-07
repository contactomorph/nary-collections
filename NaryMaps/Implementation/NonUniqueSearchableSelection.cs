using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class NonUniqueSearchableSelection<TDataTuple, TDataEntry, TComparerTuple, THandler, TSchema, T> :
    SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>,
    ISelection<TSchema, CompositeKind.Searchable, T>,
    IReadOnlySet<T>,
    IReadOnlyDictionary<T, IEnumerable<TDataTuple>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where THandler : struct, IHashTableProvider, IDataEquator<TDataEntry, TComparerTuple, T>, IResizeHandler<TDataEntry, MultiIndex>
    where TSchema : Schema<TDataTuple>, new()
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected NonUniqueSearchableSelection(NaryMapCore<TDataEntry, TComparerTuple> map) : base(map) { }

    #region Implement IReadOnlyCollection<T>

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Keys.GetEnumerator();

    public int Count => GetHandler().GetHashEntryCount();

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

    public IEnumerator<KeyValuePair<T, IEnumerable<TDataTuple>>> GetEnumerator()
    {
        THandler handler = GetHandler();
        HashEntry[] hashTable = handler.GetHashTable();
        uint expectedVersion = _map._version;
        var dataTable = _map._dataTable;

        foreach (var entry in hashTable)
        {
            if (entry.DriftPlusOne == HashEntry.DriftForUnused)
                continue;
            T item = GetItem(dataTable[entry.ForwardIndex]);

            IEnumerable<TDataTuple> dataTuples = GetRelatedDataTuples(
                handler,
                dataTable,
                expectedVersion,
                entry.ForwardIndex);

            yield return new(item, dataTuples);

            if (expectedVersion != _map._version)
                throw new InvalidOperationException("The map was modified after the enumerator was created.");
        }
    }

    public IEnumerable<T> Keys
    {
        get
        {
            HashEntry[] hashTable = GetHandler().GetHashTable();
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

    public IEnumerable<IEnumerable<TDataTuple>> Values
    {
        get
        {
            foreach (var pair in this)
                yield return pair.Value;
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

    public IEnumerable<TDataTuple> this[T key]
    {
        get
        {
            if (TryGetValue(key, out var values))
                return values;
            throw new KeyNotFoundException();
        }
    }
    
    public bool TryGetValue(T key, out IEnumerable<TDataTuple> value)
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
            value = GetRelatedDataTuples(
                handler,
                _map._dataTable,
                _map._version,
                result.ForwardIndex);
            return true;
        }

        value = null!;
        return false;
    }

    #endregion

    private IEnumerable<TDataTuple> GetRelatedDataTuples(
        THandler handler,
        TDataEntry[] dataTable,
        uint expectedVersion,
        int dataIndex)
    {
        while (dataIndex != MultiIndex.NoNext)
        {
            if (expectedVersion != _map._version)
                throw new InvalidOperationException("The map was modified after the enumerator was created.");
            
            int next = handler.GetBackIndex(dataTable, dataIndex).Next;
            yield return GetDataTuple(dataTable[dataIndex]);
            
            dataIndex = next;
        }
    }
}