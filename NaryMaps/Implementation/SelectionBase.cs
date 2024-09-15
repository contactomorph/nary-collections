using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class SelectionBase<TDataTuple, T> : IEqualityComparer<T>
    where TDataTuple : struct, ITuple, IStructuralEquatable
{
    public abstract bool Equals(T? x, T? y);
    public abstract int GetHashCode(T item);
    
    public abstract IEnumerator GetPairEnumerator();
    public abstract IEnumerator<T> GetKeyEnumerator();
    public abstract int GetKeyCount();
    public abstract bool ContainsAsKey(T item);
}

public abstract class SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T> :
    SelectionBase<TDataTuple, T>, IReadOnlySet<T>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where THandler : struct, IHashTableProvider
{
    // ReSharper disable once InconsistentNaming
    protected readonly NaryMapCore<TDataEntry, TComparerTuple> _map;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected SelectionBase(NaryMapCore<TDataEntry, TComparerTuple> map) => _map = map;
    
    #region Implement IEqualityComparer<T>
    
    public sealed override bool Equals(T? x, T? y) => EqualsUsing(_map._comparerTuple, x, y);

    public sealed override int GetHashCode(T obj) => (int)GetHashCodeUsing(_map._comparerTuple, obj);

    #endregion

    #region Implement IReadOnlyCollection<T>
    
    IEnumerator IEnumerable.GetEnumerator() => GetPairEnumerator();
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetKeyEnumerator();
    
    public int Count => GetKeyCount();

    #endregion
    
    #region Implement IReadOnlySet<T>

    public bool Contains(T item) => ContainsAsKey(item);

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var providedItems = other.ToHashSet(comparer: this);
        foreach (var dataTuple in this)
        {
            if (!providedItems.Remove(dataTuple))
                return false;
        }

        return 0 < providedItems.Count;
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var commonItems = new HashSet<T>(comparer: this);
        foreach (var dataTuple in other)
        {
            if (!Contains(dataTuple))
                return false;
            commonItems.Add(dataTuple);
        }
        return commonItems.Count < this.Count;
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var providedItems = other.ToHashSet(comparer: this);
        foreach (var dataTuple in this)
            if (!providedItems.Remove(dataTuple))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var dataTuple in other)
            if (!Contains(dataTuple))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var dataTuple in other)
            if (Contains(dataTuple))
                return true;
        return false;
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        var commonItems = new HashSet<T>(comparer: this);
        foreach (var dataTuple in other)
        {
            if (Contains(dataTuple))
                commonItems.Add(dataTuple);
            else
                return false;
        }

        foreach (var dataTuple in this)
        {
            if (!commonItems.Contains(dataTuple))
                return false;
        }
        return true;
    }

    #endregion
    
    #region Defined in derived classes as generated il
    public abstract THandler GetHandler();
    public abstract T GetItem(TDataEntry dataEntry);
    public abstract TDataTuple GetDataTuple(TDataEntry dataEntry);
    public abstract uint GetHashCodeUsing(TComparerTuple comparerTuple, T key);
    public abstract bool EqualsUsing(TComparerTuple comparerTuple, T? x, T? y);
    #endregion
}