using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ProxyReadOnlySet<TDataTuple, TKey>(SelectionBase<TDataTuple, TKey> selection) : IReadOnlySet<TKey>
    where TDataTuple : struct, ITuple, IStructuralEquatable
{
    #region Implement IReadOnlyCollection<T>
    
    IEnumerator IEnumerable.GetEnumerator() => selection.GetItemEnumerable().GetEnumerator();
    
    IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => selection.GetItemEnumerable().GetEnumerator();
    
    public int Count => selection.GetKeyCount();

    #endregion
    
    #region Implement IReadOnlySet<T>

    public bool Contains(TKey item) => selection.ContainsItem(item);

    public bool IsProperSubsetOf(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var providedItems = other.ToHashSet(comparer: selection);
        foreach (var dataTuple in this)
        {
            if (!providedItems.Remove(dataTuple))
                return false;
        }

        return 0 < providedItems.Count;
    }

    public bool IsProperSupersetOf(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var commonItems = new HashSet<TKey>(comparer: selection);
        foreach (var item in other)
        {
            if (!selection.ContainsItem(item))
                return false;
            commonItems.Add(item);
        }
        return commonItems.Count < selection.GetKeyCount();
    }

    public bool IsSubsetOf(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var providedItems = other.ToHashSet(comparer: selection);
        foreach (var item in this)
            if (!providedItems.Remove(item))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var item in other)
            if (!selection.ContainsItem(item))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var item in other)
            if (selection.ContainsItem(item))
                return true;
        return false;
    }

    public bool SetEquals(IEnumerable<TKey> other)
    {
        var commonItems = new HashSet<TKey>(comparer: selection);
        foreach (var item in other)
        {
            if (selection.ContainsItem(item))
                commonItems.Add(item);
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
}