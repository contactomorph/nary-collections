using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class NonUniqueSearchableSelection<TDataTuple, TDataEntry, TComparerTuple, THandler, TSchema, T> :
    SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>,
    ISelection<TSchema, CompositeKind.Searchable, T>
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

    public sealed override int GetKeyCount() => GetHandler().GetHashEntryCount();
    
    public sealed override TDataTuple? GetFirstDataTupleFor(T item)
    {
        THandler handler = GetHandler();
        HashEntry[] hashTable = handler.GetHashTable();
        
        uint hc = GetHashCodeUsing(_map._comparerTuple, item);
        var result = MembershipHandling<TDataEntry, TComparerTuple, T, THandler>.Find(
            hashTable,
            _map._dataTable,
            handler,
            _map._comparerTuple,
            hc,
            item);

        return result.Case == SearchCase.ItemFound ? GetDataTuple(_map._dataTable[result.ForwardIndex]) : null;
    }
    
    public sealed override IEnumerable<TDataTuple>? GetDataTuplesFor(T item)
    {
        THandler handler = GetHandler();
        HashEntry[] hashTable = handler.GetHashTable();
        
        uint hc = GetHashCodeUsing(_map._comparerTuple, item);
        var result = MembershipHandling<TDataEntry, TComparerTuple, T, THandler>.Find(
            hashTable,
            _map._dataTable,
            handler,
            _map._comparerTuple,
            hc,
            item);
        
        return result.Case == SearchCase.ItemFound ?
            GetRelatedDataTuples(handler, _map._dataTable, _map._version, result.ForwardIndex) :
            null;
    }
    
    public sealed override IEnumerable<T> GetItemEnumerable()
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

    public sealed override IEnumerable<KeyValuePair<T, IEnumerable<TDataTuple>>> GetItemAndDataTuplesEnumerable()
    {
        THandler handler = GetHandler();
        HashEntry[] hashTable = handler.GetHashTable();
        uint expectedVersion = _map._version;
        var dataTable = _map._dataTable;

        foreach (var entry in hashTable)
        {
            if (entry.DriftPlusOne == HashEntry.DriftForUnused)
                continue;
            T key = GetItem(dataTable[entry.ForwardIndex]);

            IEnumerable<TDataTuple> dataTuples = GetRelatedDataTuples(
                handler,
                dataTable,
                expectedVersion,
                entry.ForwardIndex);

            yield return new(key, dataTuples);

            if (expectedVersion != _map._version)
                throw new InvalidOperationException("The map was modified after the enumerator was created.");
        }
    }
    
    public sealed override bool RemoveAllAt(T key)
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

        if (result.Case != SearchCase.ItemFound)
            return false;

        int hashIndex = (int)result.ReducedHashCode;
        
        ++_map._version;

        while (true)
        {
            // The same index in the hashTable (hashIndex) remains valid as long as there are still dataTuples
            // corresponding to the current key.
            var entry = hashTable[hashIndex];
            if (entry.DriftPlusOne == HashEntry.DriftForUnused)
                return true;
            int dataIndex = entry.ForwardIndex;
            
            // We can check here if we are about to remove the last tuple corresponding to current key.
            var currentDataIndexIsLast = handler.GetBackIndex(_map._dataTable, dataIndex).Next == MultiIndex.NoNext;

            MustPointToAppropriateData(_map._dataTable, handler, dataIndex, _map._comparerTuple, key, hc);
            
            _map.RemoveDataAt(dataIndex);
            
            if (currentDataIndexIsLast)
                return true;
        }
    }

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
    
    [Conditional("DEBUG")]
    private static void MustPointToAppropriateData(
        TDataEntry[] dataTable,
        THandler equator,
        int dataIndex,
        TComparerTuple comparerTuple,
        T candidateItem,
        uint candidateHashCode)
    {
        Debug.Assert(
            equator.AreDataEqualAt(dataTable, comparerTuple, dataIndex, candidateItem, candidateHashCode), 
            "Provided index should still point to appropriate data.");
    }
}