using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Primitives;

namespace NaryMaps.Components;

public static class MembershipHandling<TDataEntry, TComparerTuple, T, TEquator>
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where TEquator : struct, IDataEquator<TDataEntry, TComparerTuple, T>
{
    public static SearchResult Find(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TEquator equator,
        TComparerTuple comparerTuple,
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
            if (equator.AreDataEqualAt(dataTable, comparerTuple, occupiedDataIndex, candidateItem, candidateHashCode))
                return SearchResult.CreateForItemFound(reducedHashCode, driftPlusOne, occupiedDataIndex);

            HashCodeReduction.MoveReducedHashCode(ref reducedHashCode, hashTable.Length);
            driftPlusOne++;
        }
    }
}