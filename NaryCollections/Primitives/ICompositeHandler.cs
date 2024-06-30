using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections.Primitives;

public interface ICompositeHandler<TDataTuple, THashTuple, TIndexTuple, in TComparerTuple, in T>
    where TDataTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
    where TComparerTuple : struct, ITuple, IStructuralEquatable
{
    SearchResult Contains(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        TComparerTuple comparerTuple,
        uint candidateHashCode,
        T candidateItem);

    void Add(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        TComparerTuple comparerTuple,
        SearchResult lastSearchResult,
        int candidateDataIndex);

    void Remove(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        TComparerTuple comparerTuple,
        int dataIndex,
        int dataCount);
}