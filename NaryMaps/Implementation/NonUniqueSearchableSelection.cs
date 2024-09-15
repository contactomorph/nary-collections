using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class NonUniqueSearchableSelection<TDataTuple, TDataEntry, TComparerTuple, THandler, TSchema, T> :
    SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>,
    ISelection<TSchema, CompositeKind.Searchable, T>,
    IReadOnlyDictionary<T, IEnumerable<TDataTuple>>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where THandler : struct, IHashTableProvider, IDataEquator<TDataEntry, TComparerTuple, T>, IResizeHandler<TDataEntry, MultiIndex>
    where TSchema : Schema<TDataTuple>, new()
#if !NET6_0_OR_GREATER
    where T : notnull
#endif
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected NonUniqueSearchableSelection(NaryMapCore<TDataEntry, TComparerTuple> map) : base(map) { }

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
            IReadOnlyDictionary<T, IEnumerable<TDataTuple>> that = this;
            foreach (var pair in that)
                yield return pair.Value;
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
    
    public sealed override IEnumerator<T> GetKeyEnumerator() => Keys.GetEnumerator();
    public sealed override IEnumerator GetPairEnumerator()
    {
        IReadOnlyDictionary<T, IEnumerable<TDataTuple>> that = this;
        return that.GetEnumerator();
    }
    public sealed override int GetKeyCount() => GetHandler().GetHashEntryCount();
    public sealed override bool ContainsAsKey(T item) => ContainsKey(item);
    
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