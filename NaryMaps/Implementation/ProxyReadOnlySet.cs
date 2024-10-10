using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Implementation;

public sealed class ProxyReadOnlySet<TDataTuple, TKey> : IReadOnlySet<TKey>
    where TDataTuple : struct, ITuple, IStructuralEquatable
{
    private readonly SelectionBase<TDataTuple, TKey> _selection;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxyReadOnlySet(SelectionBase<TDataTuple, TKey> selection) => _selection = selection;

    #region Implement IReadOnlyCollection<T>
    
    IEnumerator IEnumerable.GetEnumerator() => _selection.GetItemEnumerable().GetEnumerator();
    
    IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => _selection.GetItemEnumerable().GetEnumerator();
    
    public int Count => _selection.GetKeyCount();

    #endregion
    
    #region Implement IReadOnlySet<T>

    public bool Contains(TKey item) => _selection.ContainsItem(item);

    public bool IsProperSubsetOf(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var providedItems = other.ToHashSet(comparer: _selection);
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
        var commonItems = new HashSet<TKey>(comparer: _selection);
        foreach (var item in other)
        {
            if (!_selection.ContainsItem(item))
                return false;
            commonItems.Add(item);
        }
        return commonItems.Count < _selection.GetKeyCount();
    }

    public bool IsSubsetOf(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var providedItems = other.ToHashSet(comparer: _selection);
        foreach (var item in this)
            if (!providedItems.Remove(item))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var item in other)
            if (!_selection.ContainsItem(item))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<TKey> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var item in other)
            if (_selection.ContainsItem(item))
                return true;
        return false;
    }

    public bool SetEquals(IEnumerable<TKey> other)
    {
        var commonItems = new HashSet<TKey>(comparer: _selection);
        foreach (var item in other)
        {
            if (_selection.ContainsItem(item))
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