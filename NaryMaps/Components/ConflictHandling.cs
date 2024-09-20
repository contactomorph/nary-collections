using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Primitives;

namespace NaryMaps.Components;

public static class ConflictHandling<TDataTuple, THashTuple, TIndexTuple, THandler, TComparerTuple, T>
    where TDataTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
    where TComparerTuple: struct, ITuple, IStructuralEquatable
    where THandler: struct, ICompositeHandler<TDataTuple, THashTuple, TIndexTuple, TComparerTuple, T>
{
    public static void ExtractDataTupleWithSameItem(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        THandler handler,
        TComparerTuple comparerTuple,
        uint candidateHashCode,
        T candidateItem,
        List<TDataTuple> conflictingDataTuples)
    {
        var result = handler.Find(dataTable, comparerTuple, candidateHashCode, candidateItem);
        if (result.Case == SearchCase.ItemFound)
        {
            conflictingDataTuples.Add(dataTable[result.ForwardIndex].DataTuple);
        }
    }
}