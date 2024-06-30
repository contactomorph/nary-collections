using NaryCollections.Primitives;

namespace NaryCollections.Components;

internal static class MembershipHandling<TDataEntry, T> where TDataEntry : struct
{
    public static SearchResult ContainsForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IDataEquator<TDataEntry, T> projector,
        uint candidateHashCode,
        T candidateItem)
    {
        uint reducedHashCode = HashCodeReduction.ComputeReducedHashCode(candidateHashCode, hashTable.Length);
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

            HashCodeReduction.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
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
        uint reducedHashCode = HashCodeReduction.ComputeReducedHashCode(candidateHashCode, hashTable.Length);
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
            } while (occupiedCorrespondenceIndex != CorrespondenceEntry.NoNextCorrespondence);

            HashCodeReduction.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }
}