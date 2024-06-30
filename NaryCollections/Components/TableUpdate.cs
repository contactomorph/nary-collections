using NaryCollections.Primitives;

namespace NaryCollections.Components;

internal static class TableUpdate<TDataEntry> where TDataEntry : struct
{
    public static void AddForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IResizeHandler<TDataEntry> projector,
        SearchResult lastSearchResult,
        int candidateDataIndex)
    {
        uint candidateReducedHashCode = (uint)lastSearchResult.HashIndex;
        uint candidateDriftPlusOne = lastSearchResult.DriftPlusOne;

        if (lastSearchResult.Case == SearchCase.EmptyEntryFound)
        {
            hashTable[candidateReducedHashCode] = new HashEntry
            {
                DriftPlusOne = candidateDriftPlusOne,
                ForwardIndex = candidateDataIndex,
            };
            projector.SetBackIndex(dataTable, candidateDataIndex, (int)candidateReducedHashCode);
            return;
        }
        
        while (true)
        {
            var occupiedDriftPlusOne = hashTable[candidateReducedHashCode].DriftPlusOne;
            // we have reached an empty place: the item can be set here
            if (occupiedDriftPlusOne == HashEntry.DriftForUnused)
            {
                hashTable[candidateReducedHashCode] = new HashEntry
                {
                    DriftPlusOne = candidateDriftPlusOne,
                    ForwardIndex = candidateDataIndex,
                };
                projector.SetBackIndex(dataTable, candidateDataIndex, (int)candidateReducedHashCode);
                return;
            }
            
            // we have drifted too long: we must swap the current data with the candidate data
            if (occupiedDriftPlusOne < candidateDriftPlusOne)
            {
                int forwardIndex = hashTable[candidateReducedHashCode].ForwardIndex;
                hashTable[candidateReducedHashCode].ForwardIndex = candidateDataIndex;
                hashTable[candidateReducedHashCode].DriftPlusOne = candidateDriftPlusOne;
                projector.SetBackIndex(dataTable, candidateDataIndex, (int)candidateReducedHashCode);

                candidateDataIndex = forwardIndex;
                candidateDriftPlusOne = occupiedDriftPlusOne;
            }
            
            HashCodeReduction.MoveReducedHashCode(ref candidateReducedHashCode, hashTable.Length);
            candidateDriftPlusOne++;
        }
    }
    
    public static void RemoveForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IResizeHandler<TDataEntry> projector,
        int dataIndex,
        int dataCount)
    {
        uint reducedHashCode = (uint)projector.GetBackIndex(dataTable, dataIndex);
        uint nextReducedHashCode = reducedHashCode;
        HashCodeReduction.MoveReducedHashCode(ref nextReducedHashCode, hashTable.Length);

        while (true)
        {
            if (hashTable[nextReducedHashCode].DriftPlusOne <= HashEntry.Optimal)
            {
                hashTable[reducedHashCode] = default;
                break;
            }

            hashTable[reducedHashCode] = hashTable[nextReducedHashCode];
            hashTable[reducedHashCode].DriftPlusOne--;

            int forwardIndex = hashTable[reducedHashCode].ForwardIndex;
            projector.SetBackIndex(dataTable, forwardIndex, (int)reducedHashCode);

            reducedHashCode = nextReducedHashCode;
            HashCodeReduction.MoveReducedHashCode(ref nextReducedHashCode, hashTable.Length);
        }
        
        int lastDataIndex = dataCount - 1;
        if (dataIndex < lastDataIndex)
        {
            var backIndex = projector.GetBackIndex(dataTable, lastDataIndex);
            hashTable[backIndex].ForwardIndex = dataIndex;
        }
    }

    public static void ChangeCapacityForUnique(
        ref HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IResizeHandler<TDataEntry> projector,
        int newHashTableCapacity,
        int count)
    {
        hashTable = new HashEntry[newHashTableCapacity];
        
        for (int i = 0; i < count; i++)
        {
            var hashCode = projector.GetHashCodeAt(dataTable, i);
            var reducedHashCode = HashCodeReduction.ComputeReducedHashCode(hashCode, newHashTableCapacity);
            var searchResult = hashTable[reducedHashCode].DriftPlusOne == HashEntry.DriftForUnused ?
                SearchResult.CreateForEmptyEntry(reducedHashCode, HashEntry.Optimal) :
                SearchResult.CreateWhenSearchStopped(reducedHashCode, HashEntry.Optimal);
            AddForUnique(hashTable, dataTable, projector, searchResult, i);
        }
    }
}