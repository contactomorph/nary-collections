using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class UniqueSearchableSelection<TDataTuple, TDataEntry, TComparerTuple, THandler, TSchema, T> :
    SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>,
    ISelection<TSchema, CompositeKind.UniqueSearchable, T>
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

    public sealed override int GetKeyCount() => _map._count;
    
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
        
        return null;
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
            return [ GetDataTuple(_map._dataTable[result.ForwardIndex]) ];
        return null;
    }
    
    public sealed override IEnumerable<T> GetItemEnumerable()
    {
        uint expectedVersion = _map._version;
        var dataTable = _map._dataTable;
        
        for (int i = 0; i < _map._count; i++)
        {
            yield return GetItem(dataTable[i]);
            if (expectedVersion != _map._version)
                throw new InvalidOperationException("The map was modified after the enumerator was created.");
        }
    }
    
    public override IEnumerable<KeyValuePair<T, IEnumerable<TDataTuple>>> GetItemAndDataTuplesEnumerable()
    {
        uint expectedVersion = _map._version;
        var dataTable = _map._dataTable;
        
        for (int i = 0; i < _map._count; i++)
        {
            T item = GetItem(dataTable[i]);
            TDataTuple dataTuple = GetDataTuple(dataTable[i]);
            yield return new(item, [ dataTuple ]);
            if (expectedVersion != _map._version)
                throw new InvalidOperationException("The map was modified after the enumerator was created.");
        }
    }
}