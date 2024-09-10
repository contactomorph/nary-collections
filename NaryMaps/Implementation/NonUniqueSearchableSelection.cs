using System.Collections;
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
        
        if (result.Case == SearchCase.ItemFound)
            return GetDataTuple(_map._dataTable[result.ForwardIndex]);
        
        return default!;
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
        
        if (result.Case == SearchCase.ItemFound)
            return GetRelatedDataTuples(handler, _map._dataTable, _map._version, result.ForwardIndex);
        
        return default!;
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