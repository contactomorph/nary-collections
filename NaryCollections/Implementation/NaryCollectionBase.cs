using System.Collections;
using System.Runtime.CompilerServices;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NotImplementedException = System.NotImplementedException;

namespace NaryCollections.Implementation;

public abstract class NaryCollectionBase<TDataTuple, THashTuple, TIndexTuple, TComparerTuple, TCompositeHandler, TSchema>
    : INaryCollection<TSchema>, IConflictingSet<TDataTuple>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where TCompositeHandler : struct, ICompositeHandler<TDataTuple, THashTuple, TIndexTuple, TComparerTuple, TDataTuple>
    where TSchema : Schema<TDataTuple>, new()
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly TComparerTuple _comparerTuple;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once MemberCanBePrivate.Global
    protected DataEntry<TDataTuple, THashTuple, TIndexTuple>[] _dataTable;
    private TCompositeHandler _compositeHandler;
    private int _count;
    private uint _version;

    public TSchema Schema { get; }

    // ReSharper disable once ConvertToAutoProperty
    public int Count => _count;
    
    public bool IsReadOnly => false;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected NaryCollectionBase(
        TSchema schema,
        TCompositeHandler compositeHandler,
        TComparerTuple comparerTuple)
    {
        Schema = schema;
        _compositeHandler = compositeHandler;
        _comparerTuple = comparerTuple;
        _dataTable = new DataEntry<TDataTuple, THashTuple, TIndexTuple>[DataEntry.TableMinimalLength];
        _count = 0;
        _version = 0;
    }

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
                    "Underlying collection was modified after creation of the enumerator");
            
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
        throw new NotImplementedException();
    }

    public bool IsProperSupersetOf(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool IsSubsetOf(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public bool SetEquals(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
    }

    public void SymmetricExceptWith(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
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
        AddToOtherComposites(otherResults, candidateDataIndex, newDataCount: _count);
        
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
        RemoveFromOtherComposites([], dataIndex);

        DataHandling<TDataTuple, THashTuple, TIndexTuple>.RemoveOnlyData(
            ref _dataTable,
            dataIndex,
            ref _count);
        
        return true;
    }
    
    public void ExceptWith(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
    }

    public void IntersectWith(IEnumerable<TDataTuple> other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        foreach (var tuple in other)
            Remove(tuple);
    }

    public void UnionWith(IEnumerable<TDataTuple> other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        foreach (var tuple in other)
            Add(tuple);
    }

    #endregion
    
    #region Implements IConflictingSet<TDataTuple>

    public bool IsConflictingWith(TDataTuple item)
    {
        throw new NotImplementedException();
    }

    public bool ForceAdd(TDataTuple item)
    {
        throw new NotImplementedException();
    }

    #endregion
    
    #region Implements INaryCollection<TSchema>
    
    public IRelationSelection<TSchema, T> With<T>(Func<TSchema, SearchableComposite<T>> selector)
    {
        throw new NotImplementedException();
    }

    public IRelationSelection<TSchema, T> With<T>(Func<TSchema, SearchableParticipant<T>> selector)
    {
        throw new NotImplementedException();
    }

    public IOrderedRelationSelection<TSchema, T> With<T>(Func<TSchema, OrderedComposite<T>> selector)
    {
        throw new NotImplementedException();
    }

    public IOrderedRelationSelection<TSchema, T> With<T>(Func<TSchema, OrderedParticipant<T>> selector)
    {
        throw new NotImplementedException();
    }
    
    #endregion

    protected abstract THashTuple ComputeHashTuple(TDataTuple dataTuple);
    
    protected abstract bool FindInOtherComposites(
        TDataTuple dataTuple,
        THashTuple hashTuple,
        out SearchResult[] otherResults);

    protected abstract void AddToOtherComposites(
        SearchResult[] otherResults,
        int candidateDataIndex,
        int newDataCount);
    
    protected abstract void RemoveFromOtherComposites(
        SearchResult[] otherResults,
        int newDataCount);
}