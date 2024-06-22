using System.Collections;
using System.Runtime.CompilerServices;
using NaryCollections.Details;

namespace NaryCollections.Implementation;

public abstract class NaryCollectionBase<TArgTuple, THashTuple, TIndexTuple, TSchema>
    : INaryCollection<TSchema>, ISet<TArgTuple>, IReadOnlySet<TArgTuple>
    where TArgTuple : struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
    where TSchema : Schema<TArgTuple>, new()
{
    private readonly ICompleteDataProjector<TArgTuple, THashTuple, TIndexTuple> _completeProjector;
    private DataEntry<TArgTuple, THashTuple, TIndexTuple>[] _dataTable;
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
        ICompleteDataProjector<TArgTuple, THashTuple, TIndexTuple> completeProjector)
    {
        Schema = schema;
        _completeProjector = completeProjector;
        _dataTable = new DataEntry<TArgTuple, THashTuple, TIndexTuple>[DataEntry.TableMinimalLength];
        _mainHashTable = new HashEntry[DataEntry.TableMinimalLength];
        _count = 0;
        _version = 0;
    }

    #region Implements IEnumerable<TArgTuple>
    
    public IEnumerator<TArgTuple> GetEnumerator()
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
    
    #region Implements IReadOnlySet<TArgTuple>

    public void ExceptWith(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public void IntersectWith(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool IsProperSubsetOf(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool IsProperSupersetOf(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool IsSubsetOf(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool IsSupersetOf(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool Overlaps(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool SetEquals(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public void SymmetricExceptWith(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public void UnionWith(IEnumerable<TArgTuple> other)
    {
        throw new NotImplementedException();
    }

    public bool Contains(TArgTuple dataTuple)
    {
        THashTuple hashTuple = _completeProjector.ComputeHashTuple(dataTuple);
        
        var hc = (uint)hashTuple.GetHashCode();
        var result = TableHandling<DataEntry<TArgTuple, THashTuple, TIndexTuple>, TArgTuple>.ContainsForUnique(
            _mainHashTable,
            _dataTable,
            _completeProjector,
            hc,
            dataTuple);

        return result.Case == TableHandling.SearchCase.ItemFound;
    }

    #endregion
    
    #region Implements ISet<TArgTuple>

    public void CopyTo(TArgTuple[] array, int arrayIndex)
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
    
    public bool Add(TArgTuple dataTuple)
    {
        THashTuple hashTuple = _completeProjector.ComputeHashTuple(dataTuple);
        
        var hc = (uint)hashTuple.GetHashCode();
        var result = TableHandling<DataEntry<TArgTuple, THashTuple, TIndexTuple>, TArgTuple>.ContainsForUnique(
            _mainHashTable,
            _dataTable,
            _completeProjector,
            hc,
            dataTuple);
        
        if (result.Case == TableHandling.SearchCase.ItemFound)
            return false;

        ++_version;
        
        int candidateDataIndex = TableHandling<TArgTuple, THashTuple, TIndexTuple>.AddOnlyData(
            ref _dataTable,
            dataTuple,
            hashTuple,
            ref _count);

        TableHandling<DataEntry<TArgTuple, THashTuple, TIndexTuple>, TArgTuple>.AddForUnique(
            _mainHashTable,
            _dataTable,
            _completeProjector,
            result,
            candidateDataIndex);
        
        return true;
    }
    
    void ICollection<TArgTuple>.Add(TArgTuple dataTuple) => Add(dataTuple);

    public void Clear()
    {
        ++_version;
        _dataTable = new DataEntry<TArgTuple, THashTuple, TIndexTuple>[DataEntry.TableMinimalLength];
        _mainHashTable = new HashEntry[DataEntry.TableMinimalLength];
        _count = 0;
    }

    public bool Remove(TArgTuple item)
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
}