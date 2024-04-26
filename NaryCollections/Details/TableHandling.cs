namespace NaryCollections.Details;


internal static class TableHandling
{
    public static readonly uint Mask = 0x55555555;
    public static readonly uint Prime = 0x86493763;

    public static uint ComputeReducedHashCode(uint candidateHashCode, int hashTableLength)
    {
        return (candidateHashCode ^ Mask) * Prime % (uint)hashTableLength;
    }

    public static void MoveReducedHashCode(ref uint reducedHashCode, int hashTableLength)
    {
        reducedHashCode = (reducedHashCode + 1) % (uint)hashTableLength;
    }
}

internal static class TableHandling<T>
{
    public static bool ContainsForUnique(
        HashEntry[] hashTable,
        IDataProjector<T> projector,
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
                return false;
            // we have drifted too long: the item is not there, else it would have replaced the current data line
            if (occupiedDriftPlusOne < driftPlusOne)
                return false;
            // we have a good candidate for data
            int occupiedDataIndex = hashTable[reducedHashCode].ForwardIndex;
            if (projector.AreDataEqualAt(occupiedDataIndex, candidateItem, candidateHashCode))
                return true;
                
            TableHandling.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }
    
    public static bool ContainsForNonUnique(
        HashEntry[] hashTable,
        CorrespondenceEntry[] correspondenceEntries,
        IDataProjector<T> projector,
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
                return false;
            // we have drifted too long: the item is not there, else it would have replaced the current data line
            if (occupiedDriftPlusOne < driftPlusOne)
                return false;
            // we have a good candidate for data
            int occupiedCorrespondenceIndex = hashTable[reducedHashCode].ForwardIndex;

            do
            {
                // there are possible multiple lines in the correspondence table
                int occupiedDataIndex = correspondenceEntries[occupiedCorrespondenceIndex].DataIndex;
                if (projector.AreDataEqualAt(occupiedDataIndex, candidateItem, candidateHashCode))
                    return true;

                occupiedCorrespondenceIndex = correspondenceEntries[occupiedCorrespondenceIndex].Next;
            }
            while (occupiedCorrespondenceIndex != CorrespondenceEntry.NoNextCorrespondence);

            TableHandling.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }
}