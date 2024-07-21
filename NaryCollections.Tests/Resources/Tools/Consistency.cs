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

    public static void CheckForNonUnique<TDataTuple, THashTuple, TIndexTuple>(
        HashEntry[] hashTable,
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        int dataLength,
        IResizeHandler<DataEntry<TDataTuple, THashTuple, TIndexTuple>, MultiIndex> handler,
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
            MultiIndex multiIndex = handler.GetBackIndex(dataTable, i);
            if (multiIndex.IsSubsequent)
            {
                if (handler.GetBackIndex(dataTable, multiIndex.Previous).Next != i)
                    throw CreateConsistencyError("dataTable...[].Next", "dataTable...[].Previous");
                if (multiIndex.Next != MultiIndex.NoNext && !handler.GetBackIndex(dataTable, multiIndex.Next).IsSubsequent)
                    throw CreateConsistencyError("dataTable...[].Next", "dataTable...[].IsSubsequent");
            }
            else
            {
                if (hashTable[multiIndex.Previous].ForwardIndex != i)
                    throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable...[].Previous");
                if (multiIndex.Next != MultiIndex.NoNext && !handler.GetBackIndex(dataTable, multiIndex.Next).IsSubsequent)
                    throw CreateConsistencyError("dataTable...[].Next", "dataTable...[].IsSubsequent");
            }
        }
    
        for (int i = 0; i < hashTable.Length; i++)
        {
            if (hashTable[i].DriftPlusOne != HashEntry.DriftForUnused)
            {
                int forwardIndex = hashTable[i].ForwardIndex;
                if (i != handler.GetBackIndex(dataTable, forwardIndex).Previous)
                    throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable...[].Previous");
            }
        }
    }
    
    public static void EqualsAt(
        int index,
        IReadOnlyList<HashEntry> hashTable,
        uint expectedDriftPlusOne,
        int expectedForwardIndex)
    {
        var expectedEntry = new HashEntry
        {
            DriftPlusOne = expectedDriftPlusOne,
            ForwardIndex = expectedForwardIndex,
        };
        Assert.That(hashTable[index], Is.EqualTo(expectedEntry));
    }
    
    public static void AreEqualExcept(
        IReadOnlyList<HashEntry> hashTable1,
        IReadOnlyList<HashEntry> hashTable2,
        params int[] excluded)
    {
        HashSet<int> excludedIndexes = [..excluded];
        Assert.That(hashTable1.Count, Is.EqualTo(hashTable2.Count));
        for (int i = 0; i < hashTable2.Count; i++)
        {
            if (excludedIndexes.Contains(i))
                continue;
            Assert.That(hashTable1[i], Is.EqualTo(hashTable2[i]), "Hash table entries are not equal at index {0}", i);
        }
    }

    private static Exception CreateConsistencyError(string firstPlace, string secondPlace)
    {
        return new InvalidDataException($"{firstPlace} and {secondPlace} are inconsistent");
    }
}