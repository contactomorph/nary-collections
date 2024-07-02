using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections.Primitives;

public interface ICompositeHandler<TDataTuple, THashTuple, TIndexTuple, in TComparerTuple, in T>
    where TDataTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
    where TComparerTuple : struct, ITuple, IStructuralEquatable
{
    SearchResult Find(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        TComparerTuple comparerTuple,
        uint candidateHashCode,
        T candidateItem);

    void Add(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        SearchResult lastSearchResult,
        int candidateDataIndex,
        int newDataCount);

    void Remove(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        SearchResult successfulSearchResult,
        int newDataCount);
}