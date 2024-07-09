using System.Collections;
using System.Runtime.CompilerServices;
using NaryCollections.Primitives;

namespace NaryCollections.Tests.Resources.Tools;

public static class Consistency
{
    public static void CheckForUnique<TDataTuple, THashTuple, TIndexTuple>(
        HashEntry[] hashTable,
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        int dataLength,
        IResizeHandler<DataEntry<TDataTuple, THashTuple, TIndexTuple>, int> handler,
        Func<TDataTuple, THashTuple> hashTupleComputation)
        where TDataTuple: struct, ITuple, IStructuralEquatable
        where THashTuple: struct, ITuple, IStructuralEquatable
        where TIndexTuple: struct, ITuple, IStructuralEquatable
    {
        for (int i = 0; i < dataLength; i++)
        {
            var hashTuple = hashTupleComputation(dataTable[i].DataTuple);
            if (!hashTuple.Equals(dataTable[i].HashTuple))
                throw new InvalidDataException("Hash tuple is incorrect");
            int backIndex = handler.GetBackIndex(dataTable, i);
            if (i != hashTable[backIndex].ForwardIndex)
                throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable[].BackIndexesTuple");
        }

        for (int i = 0; i < hashTable.Length; i++)
        {
            if (hashTable[i].DriftPlusOne != HashEntry.DriftForUnused)
            {
                int forwardIndex = hashTable[i].ForwardIndex;
                if (i != handler.GetBackIndex(dataTable, forwardIndex))
                    throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable[].BackIndexesTuple");
            }
        }
    }

    private static Exception CreateConsistencyError(string firstPlace, string secondPlace)
    {
        return new InvalidDataException($"{firstPlace} and {secondPlace} are inconsistent");
    }
}