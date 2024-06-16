namespace NaryCollections.Details;

internal static class TableHandling
{
    public enum SearchCase
    {
        EmptyEntryFound = 0,
        SearchStopped = 1,
        ItemFound = 2,
    }
    
    public record struct SearchResult(SearchCase Case, int HashIndex, uint DriftPlusOne);

    public static SearchResult CreateForEmptyEntry(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.EmptyEntryFound, (int)reducedHash, driftPlusOne);
    }
    
    public static SearchResult CreateWhenSearchStopped(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.SearchStopped, (int)reducedHash, driftPlusOne);
    }
    
    public static SearchResult CreateForItemFound(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.ItemFound, (int)reducedHash, driftPlusOne);
    }

    public static uint ComputeReducedHashCode(uint candidateHashCode, int hashTableLength)
    {
        return candidateHashCode % (uint)hashTableLength;
    }

    public static void MoveReducedHashCode(ref uint reducedHashCode, int hashTableLength)
    {
        reducedHashCode = (reducedHashCode + 1) % (uint)hashTableLength;
    }
}

internal static class TableHandling<TDataEntry, T>
{
    public static TableHandling.SearchResult ContainsForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IDataProjector<TDataEntry, T> projector,
        uint candidateHashCode,
        T candidateItem)
    {
        uint reducedHashCode = TableHandling.ComputeReducedHashCode(candidateHashCode, hashTable.Length);
        uint driftPlusOne = 1;
        while (true)
        {
            var occupiedDriftPlusOne = hashTable[reducedHashCode].DriftPlusOne;
            // we have reached an empty place: the item is not there
            if (occupiedDriftPlusOne == HashEntry.DriftForUnused)
                return TableHandling.CreateForEmptyEntry(reducedHashCode, driftPlusOne);
            // we have drifted too long: the item is not there, else it would have replaced the current data line
            if (occupiedDriftPlusOne < driftPlusOne)
                return TableHandling.CreateWhenSearchStopped(reducedHashCode, driftPlusOne);
            // we have a good candidate for data
            int occupiedDataIndex = hashTable[reducedHashCode].ForwardIndex;
            if (projector.AreDataEqualAt(dataTable, occupiedDataIndex, candidateItem, candidateHashCode))
                return TableHandling.CreateForItemFound(reducedHashCode, driftPlusOne);
                
            TableHandling.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }
    
    public static TableHandling.SearchResult ContainsForNonUnique(
        HashEntry[] hashTable,
        CorrespondenceEntry[] correspondenceTable,
        TDataEntry[] dataTable,
        IDataProjector<TDataEntry, T> projector,
        uint candidateHashCode,
        T candidateItem)
    {
        uint reducedHashCode = TableHandling.ComputeReducedHashCode(candidateHashCode, hashTable.Length);
        uint driftPlusOne = 1;
        while (true)
        {
            var occupiedDriftPlusOne = hashTable[reducedHashCode].DriftPlusOne;
            // we have reached an empty place: the item is not there
            if (occupiedDriftPlusOne == HashEntry.DriftForUnused)
                return TableHandling.CreateForEmptyEntry(reducedHashCode, driftPlusOne);
            // we have drifted too long: the item is not there, else it would have replaced the current data line
            if (occupiedDriftPlusOne < driftPlusOne)
                return TableHandling.CreateWhenSearchStopped(reducedHashCode, driftPlusOne);
            // we have a good candidate for data
            int occupiedCorrespondenceIndex = hashTable[reducedHashCode].ForwardIndex;

            do
            {
                // there are possible multiple lines in the correspondence table
                int occupiedDataIndex = correspondenceTable[occupiedCorrespondenceIndex].DataIndex;
                if (projector.AreDataEqualAt(dataTable, occupiedDataIndex, candidateItem, candidateHashCode))
                    return TableHandling.CreateForItemFound(reducedHashCode, driftPlusOne);

                occupiedCorrespondenceIndex = correspondenceTable[occupiedCorrespondenceIndex].Next;
            }
            while (occupiedCorrespondenceIndex != CorrespondenceEntry.NoNextCorrespondence);

            TableHandling.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }
}