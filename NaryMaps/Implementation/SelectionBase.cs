using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Primitives;
#if NETSTANDARD2_1 || NETCOREAPP3_1
using NaryMaps.Tools;
#endif

namespace NaryMaps.Implementation;

public abstract class SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T> :
    IReadOnlySet<T>, IEqualityComparer<T>
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
    
    public bool Equals(T? x, T? y) => EqualsUsing(_map._comparerTuple, x, y);

    public int GetHashCode(T obj) => (int)ComputeHashCode(_map._comparerTuple, obj);

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
    
    protected abstract IEnumerator GetPairEnumerator();
    protected abstract IEnumerator<T> GetKeyEnumerator();
    protected abstract int GetKeyCount();
    protected abstract bool ContainsAsKey(T item);
    
    protected internal abstract THandler GetHandler();
    protected internal abstract T GetItem(TDataEntry dataEntry);
    protected internal abstract TDataTuple GetDataTuple(TDataEntry dataEntry);
    protected internal abstract uint ComputeHashCode(TComparerTuple comparerTuple, T item);
    protected internal abstract bool EqualsUsing(TComparerTuple comparerTuple, T? x, T? y);
}