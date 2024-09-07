using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;
using NotImplementedException = System.NotImplementedException;

namespace NaryMaps.Implementation;

public static class NaryMapBase
{
    public const string DataTableFieldName = "_dataTable";
    public const string CountFieldName = "_count";
}

public abstract class NaryMapBase<TDataTuple, THashTuple, TIndexTuple, TComparerTuple, TCompositeHandler, TSchema>
    : NaryMapCore<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TComparerTuple>,
        INaryMap<TSchema>,
        IConflictingSet<TDataTuple>,
        IEqualityComparer<TDataTuple>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where TCompositeHandler : struct, ICompositeHandler<TDataTuple, THashTuple, TIndexTuple, TComparerTuple, TDataTuple>
    where TSchema : Schema<TDataTuple>, new()
{
    private TCompositeHandler _compositeHandler;

    public TSchema Schema { get; }

    // ReSharper disable once ConvertToAutoProperty
    public int Count => _count;
    
    public bool IsReadOnly => false;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected NaryMapBase(TSchema schema, TCompositeHandler compositeHandler, TComparerTuple comparerTuple) :
        base(comparerTuple)
    {
        Schema = schema;
        _compositeHandler = compositeHandler;
    }

    #region Implements IEqualityComparer<TDataTuple>

    public abstract bool Equals(TDataTuple x, TDataTuple y);

    public int GetHashCode(TDataTuple obj) => ComputeHashTuple(obj).GetHashCode();
    
    #endregion

    #region Implements IEnumerable<TDataTuple>

    public IEnumerator<TDataTuple> GetEnumerator()
    {
        int i = 0;
        uint originalVersion = _version;
        if (_count <= i) yield break;
        foreach (var dataEntry in _dataTable)
        {
            if (originalVersion != _version)
                throw new InvalidOperationException(
                    "Underlying map was modified after creation of the enumerator");
            
            yield return dataEntry.DataTuple;
            ++i;
            if (_count <= i) yield break;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
    
    #region Implements IReadOnlySet<TDataTuple>

    public bool IsProperSubsetOf(IEnumerable<TDataTuple> other)
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

    public bool IsProperSupersetOf(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var commonItems = new HashSet<TDataTuple>(comparer: this);
        foreach (var dataTuple in other)
        {
            if (!Contains(dataTuple))
                return false;
            commonItems.Add(dataTuple);
        }
        return commonItems.Count < this.Count;
    }

    public bool IsSubsetOf(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var providedItems = other.ToHashSet(comparer: this);
        foreach (var dataTuple in this)
            if (!providedItems.Remove(dataTuple))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var dataTuple in other)
            if (!Contains(dataTuple))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var dataTuple in other)
            if (Contains(dataTuple))
                return true;
        return false;
    }

    public bool SetEquals(IEnumerable<TDataTuple> other)
    {
        var commonItems = new HashSet<TDataTuple>(comparer: this);
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

    public bool Contains(TDataTuple dataTuple)
    {
        THashTuple hashTuple = ComputeHashTuple(dataTuple);
        var hc = (uint)hashTuple.GetHashCode();
        
        ref TCompositeHandler handlerReference = ref _compositeHandler;
        var result = handlerReference.Find(_dataTable, _comparerTuple, hc, dataTuple);

        return result.Case == SearchCase.ItemFound;
    }

    #endregion
    
    #region Implements ISet<TDataTuple>

    public void CopyTo(TDataTuple[] array, int arrayIndex)
    {
        if (array is null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0) throw new IndexOutOfRangeException();
        if (array.Length - arrayIndex < _count) throw new ArgumentException(nameof(arrayIndex));
        int i = arrayIndex;
        foreach (var tuple in this)
        {
            array[i] = tuple;
            ++i;
        }
    }
    
    public bool Add(TDataTuple dataTuple)
    {
        THashTuple hashTuple = ComputeHashTuple(dataTuple);
        
        var hc = (uint)hashTuple.GetHashCode();
        
        ref TCompositeHandler handlerReference = ref _compositeHandler;
        var result = handlerReference.Find(_dataTable, _comparerTuple, hc, dataTuple);
        
        if (result.Case == SearchCase.ItemFound)
            return false;
        
        var alreadyInside = FindInOtherComposites(dataTuple, hashTuple, out var otherResults);
        if (alreadyInside)
            return false;

        ++_version;
        
        int candidateDataIndex = DataHandling<TDataTuple, THashTuple, TIndexTuple>.AddOnlyData(
            ref _dataTable,
            dataTuple,
            hashTuple,
            ref _count);

        handlerReference.Add(_dataTable, result, candidateDataIndex, newDataCount: _count);
        AddToOtherComposites(otherResults, candidateDataIndex);
        
        return true;
    }

    void ICollection<TDataTuple>.Add(TDataTuple dataTuple) => Add(dataTuple);

    public void Clear()
    {
        ++_version;
        _count = 0;
        _dataTable = new DataEntry<TDataTuple, THashTuple, TIndexTuple>[DataEntry.TableMinimalLength];
        ref TCompositeHandler handlerReference = ref _compositeHandler;
        handlerReference.Clear();
        ClearOtherComposites();
    }

    public bool Remove(TDataTuple dataTuple)
    {
        THashTuple hashTuple = ComputeHashTuple(dataTuple);
        
        var hc = (uint)hashTuple.GetHashCode();
        
        ref TCompositeHandler handlerReference = ref _compositeHandler;
        var result = handlerReference.Find(_dataTable, _comparerTuple, hc, dataTuple);
        
        if (result.Case != SearchCase.ItemFound)
            return false;

        ++_version;
        
        int dataIndex = result.ForwardIndex;
        
        handlerReference.Remove(_dataTable, dataIndex, _count);
        RemoveFromOtherComposites(dataIndex);

        DataHandling<TDataTuple, THashTuple, TIndexTuple>.RemoveOnlyData(
            ref _dataTable,
            dataIndex,
            ref _count);
        
        return true;
    }
    
    public void ExceptWith(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var tuple in other)
            Remove(tuple);
    }

    public void IntersectWith(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var commonItems = new HashSet<TDataTuple>(comparer: this);
        foreach (var dataTuple in other)
        {
            if (Contains(dataTuple))
                commonItems.Add(dataTuple);
        }

        var list = this.ToList();
        foreach (var dataTuple in list)
        {
            if (!commonItems.Contains(dataTuple))
                Remove(dataTuple);
        }
    }

    /// <summary>Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both.</summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="other" /> is <see langword="null" />.</exception>
    public void SymmetricExceptWith(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var commonItems = new HashSet<TDataTuple>(comparer: this);
        var onlyOther = new List<TDataTuple>();
        foreach (var dataTuple in other)
        {
            if (Contains(dataTuple))
                commonItems.Add(dataTuple);
            else
                onlyOther.Add(dataTuple);
        }

        var list = this.ToList();
        foreach (var dataTuple in list)
        {
            if (commonItems.Contains(dataTuple))
                Remove(dataTuple);
        }

        foreach (var dataTuple in onlyOther) Add(dataTuple);
    }

    public void UnionWith(IEnumerable<TDataTuple> other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        foreach (var tuple in other)
            Add(tuple);
    }

    #endregion
    
    #region Implements INaryMap<TSchema>
    
    public IReadOnlySet<T> AsReadOnlySet<TK, T>(Func<TSchema, ParticipantBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var participant = selector(Schema);
        if (participant is null) throw new ArgumentException(nameof(selector));
        if (participant.Schema != Schema) throw new ArgumentException(nameof(selector));
        return (IReadOnlySet<T>)CreateSelection(participant.Rank);
    }

    public IReadOnlySet<T> AsReadOnlySet<TK, T>(Func<TSchema, CompositeBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
    {
        throw new NotImplementedException();
    }

    public ISelection<TSchema, TK, T> With<TK, T>(Func<TSchema,  ParticipantBase<TK, T>> selector)
        where TK : CompositeKind.Basic
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var participant = selector(Schema);
        if (participant is null) throw new ArgumentException(nameof(selector));
        if (participant.Schema != Schema) throw new ArgumentException(nameof(selector));
        return (ISelection<TSchema, TK, T>)CreateSelection(participant.Rank);
    }
    
    public ISelection<TSchema, TK, T> With<TK, T>(Func<TSchema, CompositeBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
    {
        throw new NotImplementedException();
    }
    
    #endregion

    protected abstract THashTuple ComputeHashTuple(TDataTuple dataTuple);
    
    protected abstract bool FindInOtherComposites(
        TDataTuple dataTuple,
        THashTuple hashTuple,
        out SearchResult[] otherResults);

    protected abstract void AddToOtherComposites(SearchResult[] otherResults, int candidateDataIndex);
    
    protected abstract void RemoveFromOtherComposites(int removedDataIndex);

    protected abstract void ClearOtherComposites();

    protected abstract object CreateSelection(byte rank);
}