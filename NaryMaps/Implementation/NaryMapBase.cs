using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Components;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

internal static class NaryMapBase
{
    public const string DataTableFieldName = "_dataTable";
    public const string CountFieldName = "_count";
}

///<exclude/>
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
    
    #region Implements IConflictingSet<TSchema>
    
    public bool ForceAdd(TDataTuple dataTuple)
    {
        if (Contains(dataTuple))
            return false;
        List<TDataTuple> conflictingItems = GetConflictingItemsWith(dataTuple);

        foreach (var conflictingItem in conflictingItems)
            Remove(conflictingItem);
        Add(dataTuple);
        return true;
    }

    public bool IsConflictingWith(TDataTuple dataTuple)
    {
        THashTuple hashTuple = ComputeHashTuple(dataTuple);

        var hc = (uint)hashTuple.GetHashCode();

        ref TCompositeHandler handlerReference = ref _compositeHandler;
        var result = handlerReference.Find(_dataTable, _comparerTuple, hc, dataTuple);

        if (result.Case == SearchCase.ItemFound)
            return true;

        return FindInOtherComposites(dataTuple, hashTuple, out _);
    }

    public List<TDataTuple> GetConflictingItemsWith(TDataTuple dataTuple)
    {
        THashTuple hashTuple = ComputeHashTuple(dataTuple);
        var hc = (uint)hashTuple.GetHashCode();

        List<TDataTuple> conflictingTuples = new();
        
        ConflictHandling<TDataTuple, THashTuple, TIndexTuple, TCompositeHandler, TComparerTuple, TDataTuple>
            .ExtractDataTupleWithSameItem(
                _dataTable,
                _compositeHandler,
                _comparerTuple,
                hc,
                dataTuple,
                conflictingTuples);
        
        if (0 < conflictingTuples.Count)
            return conflictingTuples;

        ExtractConflictingItemsInOtherComposites(dataTuple, hashTuple, conflictingTuples);

        return conflictingTuples;
    }
    
    #endregion
    
    #region Implements INaryMap<TSchema>
    
    public IReadOnlySet<T> AsReadOnlySet<TK, T>(Func<TSchema, ParticipantBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var participant = selector(Schema);
        if (participant is null) throw new ArgumentException(nameof(selector));
        if (!ReferenceEquals(participant.Schema, Schema)) throw new ArgumentException(nameof(selector));
        var selection = (IReadOnlySelection<TSchema, TK, T>)CreateSelection(participant.Rank);
        return selection.AsReadOnlySet();
    }

    public IReadOnlySet<T> AsReadOnlySet<TK, T>(Func<TSchema, CompositeBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var composite = selector(Schema);
        if (composite is null) throw new ArgumentException(nameof(selector));
        if (!ReferenceEquals(composite.Schema, Schema)) throw new ArgumentException(nameof(selector));
        var selection = (IReadOnlySelection<TSchema, TK, T>)CreateSelection(composite.Rank);
        return selection.AsReadOnlySet();
    }

    public IReadOnlySelection<TSchema, TK, T> With<TK, T>(Func<TSchema,  ParticipantBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var participant = selector(Schema);
        if (participant is null) throw new ArgumentException(nameof(selector));
        if (participant.Schema != Schema) throw new ArgumentException(nameof(selector));
        return (IReadOnlySelection<TSchema, TK, T>)CreateSelection(participant.Rank);
    }
    
    public IReadOnlySelection<TSchema, TK, T> With<TK, T>(Func<TSchema, CompositeBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var composite = selector(Schema);
        if (composite is null) throw new ArgumentException(nameof(selector));
        if (composite.Schema != Schema) throw new ArgumentException(nameof(selector));
        return (IReadOnlySelection<TSchema, TK, T>)CreateSelection(composite.Rank);
    }

    ISelection<TSchema, TK, T> INaryMap<TSchema>.With<TK, T>(Func<TSchema, ParticipantBase<TK, T>> selector)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var participant = selector(Schema);
        if (participant is null) throw new ArgumentException(nameof(selector));
        if (participant.Schema != Schema) throw new ArgumentException(nameof(selector));
        return (ISelection<TSchema, TK, T>)CreateSelection(participant.Rank);
    }

    ISelection<TSchema, TK, T> INaryMap<TSchema>.With<TK, T>(Func<TSchema, CompositeBase<TK, T>> selector)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        var participant = selector(Schema);
        if (participant is null) throw new ArgumentException(nameof(selector));
        if (participant.Schema != Schema) throw new ArgumentException(nameof(selector));
        return (ISelection<TSchema, TK, T>)CreateSelection(participant.Rank);
    }
    
    #endregion
    
    #region Implements NaryMapCore

    protected internal sealed override void RemoveDataAt(int dataIndex)
    {
        ref TCompositeHandler handlerReference = ref _compositeHandler;
        handlerReference.Remove(_dataTable, dataIndex, _count);
        RemoveFromOtherComposites(dataIndex);
        
        DataHandling<TDataTuple, THashTuple, TIndexTuple>.RemoveOnlyData(
            ref _dataTable,
            dataIndex,
            ref _count);
    }

    #endregion
    
    #region Defined in derived classes as generated il
    
    protected abstract THashTuple ComputeHashTuple(TDataTuple dataTuple);
    
    protected abstract bool FindInOtherComposites(
        TDataTuple dataTuple,
        THashTuple hashTuple,
        out SearchResult[] otherResults);

    protected abstract void AddToOtherComposites(SearchResult[] otherResults, int candidateDataIndex);
    
    protected abstract void RemoveFromOtherComposites(int removedDataIndex);

    protected abstract void ExtractConflictingItemsInOtherComposites(
        TDataTuple dataTuple,
        THashTuple hashTuple,
        List<TDataTuple> conflictingDataTuples);

    protected abstract void ClearOtherComposites();

    protected abstract object CreateSelection(byte rank);
    
    #endregion
}