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
        IDataProjector<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple> projector,
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
            int backIndex = projector.GetBackIndex(dataTable, i);
            if (i != hashTable[backIndex].ForwardIndex)
                throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable[].BackIndexesTuple");
        }

        for (int i = 0; i < hashTable.Length; i++)
        {
            if (hashTable[i].DriftPlusOne != HashEntry.DriftForUnused)
            {
                int forwardIndex = hashTable[i].ForwardIndex;
                if (i != projector.GetBackIndex(dataTable, forwardIndex))
                    throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable[].BackIndexesTuple");
            }
        }
    }
    
    public static void CheckForNonUnique<TDataTuple, THashTuple, TIndexTuple>(
        HashEntry[] hashTable,
        CorrespondenceEntry[] correspondenceTable,
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        int dataLength,
        IDataProjector<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple> projector,
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
            int correspondenceIndex = projector.GetBackIndex(dataTable, i);
            if (i != correspondenceTable[correspondenceIndex].DataIndex)
                throw CreateConsistencyError("correspondenceEntries[].DataIndex", "dataTable[].BackIndexesTuple");
            switch (correspondenceTable[correspondenceIndex].Status)
            {
                case EntryStatus.First:
                    int hashIndex = correspondenceTable[correspondenceIndex].Previous;
                    if (correspondenceIndex != hashTable[hashIndex].ForwardIndex)
                        throw CreateConsistencyError("hashTable[].ForwardIndex", "correspondenceTable[].Previous");
                    int next1 = correspondenceTable[correspondenceIndex].Next;
                    if (next1 != CorrespondenceEntry.NoNextCorrespondence && correspondenceTable[next1].Status != EntryStatus.Subsequent)
                        throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceTable[].Status");
                    break;
                case EntryStatus.Subsequent:
                    int previous = correspondenceTable[correspondenceIndex].Previous;
                    if (correspondenceIndex != correspondenceTable[previous].Next)
                        throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceTable[].Previous");
                    int next2 = correspondenceTable[correspondenceIndex].Next;
                    if (next2 != CorrespondenceEntry.NoNextCorrespondence && correspondenceTable[next2].Status != EntryStatus.Subsequent)
                        throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceTable[].Status");
                    break;
                default:
                    throw new InvalidDataException("Invalid empty correspondence");
            }
        }

        var verifiedIndex = new HashSet<int>();
        for (int i = 0; i < correspondenceTable.Length; i++)
        {
            switch (correspondenceTable[i].Status)
            {
                case EntryStatus.Unused:
                    if (!verifiedIndex.Add(i))
                        throw new InvalidDataException("Known index");
                    break;
                case EntryStatus.First:
                    if (!verifiedIndex.Add(i))
                        throw new InvalidDataException("Known index");
                    int previousJ = i;
                    int currentJ = correspondenceTable[i].Next;
                    while (currentJ != CorrespondenceEntry.NoNextCorrespondence)
                    {
                        if (!verifiedIndex.Add(currentJ))
                            throw new InvalidDataException("Known index");
                        if (correspondenceTable[currentJ].Status != EntryStatus.Subsequent)
                            throw new InvalidDataException("Invalid correspondence status");
                        if (correspondenceTable[currentJ].Previous != previousJ)
                            throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceEntries[].Previous");
                        previousJ = currentJ;
                        currentJ = correspondenceTable[currentJ].Next;
                    }
                    break;
            }
        }

        for (int i = 0; i < hashTable.Length; i++)
        {
            if (hashTable[i].DriftPlusOne != HashEntry.DriftForUnused)
            {
                int forwardIndex = hashTable[i].ForwardIndex;
                if (i != correspondenceTable[forwardIndex].Previous)
                    throw CreateConsistencyError("hashTable[].ForwardIndex", "correspondenceEntries[].Previous");
            }
        }
    }

    private static Exception CreateConsistencyError(string firstPlace, string secondPlace)
    {
        return new InvalidDataException($"{firstPlace} and {secondPlace} are inconsistent");
    }
}