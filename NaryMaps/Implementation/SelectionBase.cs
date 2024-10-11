using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class SelectionBase<TDataTuple, T> : IEqualityComparer<T>
    where TDataTuple : struct, ITuple, IStructuralEquatable
{
    #region Defined in SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>
    public abstract bool Equals(T? x, T? y);
    public abstract int GetHashCode(T item);
    public abstract int GetDataTupleCount();
    public abstract bool ContainsItem(T item);
    public abstract ISet<TDataTuple> GetMapAsSet();
    
    #endregion
    
    #region Defined in SelectionBase subclasses
        
    public abstract int GetKeyCount();
    public abstract TDataTuple? GetFirstDataTupleFor(T item);
    public abstract IEnumerable<TDataTuple>? GetDataTuplesFor(T item);
    public abstract IEnumerable<T> GetItemEnumerable();
    public abstract IEnumerable<KeyValuePair<T, IEnumerable<TDataTuple>>> GetItemAndDataTuplesEnumerable();
    
    #endregion
}

public abstract class SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T> :
    SelectionBase<TDataTuple, T>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where THandler : struct, IHashTableProvider, IDataEquator<TDataEntry, TComparerTuple, T>
{
    // ReSharper disable once InconsistentNaming
    protected readonly NaryMapCore<TDataEntry, TComparerTuple> _map;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected SelectionBase(NaryMapCore<TDataEntry, TComparerTuple> map) => _map = map;
    
    #region Implement IEqualityComparer<T>
    
    public sealed override bool Equals(T? x, T? y) => EqualsUsing(_map._comparerTuple, x, y);
    
    public sealed override int GetHashCode(T item) => (int)GetHashCodeUsing(_map._comparerTuple, item);
    
    #endregion

    #region Implement SelectionBase<TDataTuple, TKey>
    
    public sealed override int GetDataTupleCount() => _map._count;
    
    public sealed override bool ContainsItem(T key)
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

    public sealed override ISet<TDataTuple> GetMapAsSet() => (ISet<TDataTuple>)_map;

    #endregion
    
    #region Defined in derived classes as generated il
    
    public abstract THandler GetHandler();
    public abstract T GetItem(TDataEntry dataEntry);
    public abstract TDataTuple GetDataTuple(TDataEntry dataEntry);
    public abstract uint GetHashCodeUsing(TComparerTuple comparerTuple, T key);
    public abstract bool EqualsUsing(TComparerTuple comparerTuple, T? x, T? y);
    
    #endregion
}