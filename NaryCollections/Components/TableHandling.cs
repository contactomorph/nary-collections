using NaryCollections.Primitives;

namespace NaryCollections.Components;

internal static class TableHandling
{
    public static uint ComputeReducedHashCode(uint candidateHashCode, int hashTableLength)
    {
        return candidateHashCode % (uint)hashTableLength;
    }

    public static void MoveReducedHashCode(ref uint reducedHashCode, int hashTableLength)
    {
        reducedHashCode = (reducedHashCode + 1) % (uint)hashTableLength;
    }
}

internal static class TableHandling<TDataEntry, T> where TDataEntry : struct
{
    public static SearchResult ContainsForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IDataEquator<TDataEntry, T> projector,
        uint candidateHashCode,
        T candidateItem)
    {
        uint reducedHashCode = TableHandling.ComputeReducedHashCode(candidateHashCode, hashTable.Length);
        uint driftPlusOne = HashEntry.Optimal;
        while (true)
        {
            var occupiedDriftPlusOne = hashTable[reducedHashCode].DriftPlusOne;
            // we have reached an empty place: the item is not there
            if (occupiedDriftPlusOne == HashEntry.DriftForUnused)
                return SearchResult.CreateForEmptyEntry(reducedHashCode, driftPlusOne);
            // we have drifted too long: the item is not there, else it would have replaced the current data line
            if (occupiedDriftPlusOne < driftPlusOne)
                return SearchResult.CreateWhenSearchStopped(reducedHashCode, driftPlusOne);
            // we have a good candidate for data
            int occupiedDataIndex = hashTable[reducedHashCode].ForwardIndex;
            if (projector.AreDataEqualAt(dataTable, occupiedDataIndex, candidateItem, candidateHashCode))
                return SearchResult.CreateForItemFound(reducedHashCode, driftPlusOne);
                
            TableHandling.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }
    
    public static SearchResult ContainsForNonUnique(
        HashEntry[] hashTable,
        CorrespondenceEntry[] correspondenceTable,
        TDataEntry[] dataTable,
        IDataEquator<TDataEntry, T> projector,
        uint candidateHashCode,
        T candidateItem)
    {
        uint reducedHashCode = TableHandling.ComputeReducedHashCode(candidateHashCode, hashTable.Length);
        uint driftPlusOne = HashEntry.Optimal;
        while (true)
        {
            var occupiedDriftPlusOne = hashTable[reducedHashCode].DriftPlusOne;
            // we have reached an empty place: the item is not there
            if (occupiedDriftPlusOne == HashEntry.DriftForUnused)
                return SearchResult.CreateForEmptyEntry(reducedHashCode, driftPlusOne);
            // we have drifted too long: the item is not there, else it would have replaced the current data line
            if (occupiedDriftPlusOne < driftPlusOne)
                return SearchResult.CreateWhenSearchStopped(reducedHashCode, driftPlusOne);
            // we have a good candidate for data
            int occupiedCorrespondenceIndex = hashTable[reducedHashCode].ForwardIndex;

            do
            {
                // there are possible multiple lines in the correspondence table
                int occupiedDataIndex = correspondenceTable[occupiedCorrespondenceIndex].DataIndex;
                if (projector.AreDataEqualAt(dataTable, occupiedDataIndex, candidateItem, candidateHashCode))
                    return SearchResult.CreateForItemFound(reducedHashCode, driftPlusOne);

                occupiedCorrespondenceIndex = correspondenceTable[occupiedCorrespondenceIndex].Next;
            }
            while (occupiedCorrespondenceIndex != CorrespondenceEntry.NoNextCorrespondence);

            TableHandling.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }

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
            
            TableHandling.MoveReducedHashCode(ref candidateReducedHashCode, hashTable.Length);
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
        TableHandling.MoveReducedHashCode(ref nextReducedHashCode, hashTable.Length);

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
            TableHandling.MoveReducedHashCode(ref nextReducedHashCode, hashTable.Length);
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
            var reducedHashCode = TableHandling.ComputeReducedHashCode(hashCode, newHashTableCapacity);
            var searchResult = hashTable[reducedHashCode].DriftPlusOne == HashEntry.DriftForUnused ?
                SearchResult.CreateForEmptyEntry(reducedHashCode, HashEntry.Optimal) :
                SearchResult.CreateWhenSearchStopped(reducedHashCode, HashEntry.Optimal);
            AddForUnique(hashTable, dataTable, projector, searchResult, i);
        }
    }
}