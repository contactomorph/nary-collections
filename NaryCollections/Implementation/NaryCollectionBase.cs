using System.Collections;
using System.Runtime.CompilerServices;
using NaryCollections.Components;
using NaryCollections.Primitives;

namespace NaryCollections.Implementation;

public abstract class NaryCollectionBase<TDataTuple, THashTuple, TIndexTuple, TProjector, TSchema>
    : INaryCollection<TSchema>, ISet<TDataTuple>, IReadOnlySet<TDataTuple>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
    where TProjector : struct, IDataProjector<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>
    where TSchema : Schema<TDataTuple>, new()
{
    private readonly TProjector _completeProjector;
    private DataEntry<TDataTuple, THashTuple, TIndexTuple>[] _dataTable;
    private HashEntry[] _mainHashTable;
    private int _count;
    private uint _version;

    public TSchema Schema { get; }

    // ReSharper disable once ConvertToAutoProperty
    public int Count => _count;
    
    public bool IsReadOnly => false;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected NaryCollectionBase(
        TSchema schema,
        TProjector completeProjector)
    {
        Schema = schema;
        _completeProjector = completeProjector;
        _dataTable = new DataEntry<TDataTuple, THashTuple, TIndexTuple>[DataEntry.TableMinimalLength];
        _mainHashTable = new HashEntry[HashEntry.TableMinimalLength];
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

    public void ExceptWith(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
    }

    public void IntersectWith(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
    }

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
        throw new NotImplementedException();
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

    public void UnionWith(IEnumerable<TDataTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool Contains(TDataTuple dataTuple)
    {
        THashTuple hashTuple = ComputeHashTuple(dataTuple);
        
        var hc = (uint)hashTuple.GetHashCode();
        var result = TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.ContainsForUnique(
            _mainHashTable,
            _dataTable,
            _completeProjector,
            hc,
            dataTuple);

        return result.Case == TableHandling.SearchCase.ItemFound;
    }

    #endregion
    
    #region Implements ISet<TDataTuple>

    public void CopyTo(TDataTuple[] array, int arrayIndex)
    {
        if (array is null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0) throw new IndexOutOfRangeException();
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
        var result = TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.ContainsForUnique(
            _mainHashTable,
            _dataTable,
            _completeProjector,
            hc,
            dataTuple);
        
        if (result.Case == TableHandling.SearchCase.ItemFound)
            return false;

        ++_version;
        
        int candidateDataIndex = TableHandling<TDataTuple, THashTuple, TIndexTuple>.AddOnlyData(
            ref _dataTable,
            dataTuple,
            hashTuple,
            ref _count);

        if (HashEntry.IsFullEnough(_mainHashTable.Length, _count))
        {
            TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.ChangeCapacityForUnique(
                ref _mainHashTable,
                _dataTable,
                _completeProjector,
                newHashTableCapacity: HashEntry.IncreaseCapacity(_mainHashTable.Length),
                _count);
        }
        else
        {
            TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.AddForUnique(
                _mainHashTable,
                _dataTable,
                _completeProjector,
                result,
                candidateDataIndex);
        }
        
        return true;
    }
    
    void ICollection<TDataTuple>.Add(TDataTuple dataTuple) => Add(dataTuple);

    public void Clear()
    {
        ++_version;
        _dataTable = new DataEntry<TDataTuple, THashTuple, TIndexTuple>[DataEntry.TableMinimalLength];
        _mainHashTable = new HashEntry[DataEntry.TableMinimalLength];
        _count = 0;
    }

    public bool Remove(TDataTuple dataTuple)
    {
        THashTuple hashTuple = ComputeHashTuple(dataTuple);
        
        var hc = (uint)hashTuple.GetHashCode();
        var result = TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.ContainsForUnique(
            _mainHashTable,
            _dataTable,
            _completeProjector,
            hc,
            dataTuple);
        
        if (result.Case != TableHandling.SearchCase.ItemFound)
            return false;

        ++_version;

        int dataIndex = _mainHashTable[result.HashIndex].ForwardIndex;
        
        TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.RemoveForUnique(
            _mainHashTable,
            _dataTable,
            _completeProjector,
            dataIndex,
            _count);

        if (HashEntry.IsSparseEnough(_mainHashTable.Length, _count))
        {
            TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.ChangeCapacityForUnique(
                ref _mainHashTable,
                _dataTable,
                _completeProjector,
                newHashTableCapacity: HashEntry.DecreaseCapacity(_mainHashTable.Length),
                _count);
        }
        else
        {
            TableHandling<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>.RemoveOnlyData(
                ref _dataTable, 
                dataIndex,
                ref _count);
        }
        
        return true;
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
}