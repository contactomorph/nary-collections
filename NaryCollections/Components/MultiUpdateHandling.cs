using System.Runtime.CompilerServices;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

public static class MultiUpdateHandling<TDataEntry, TResizeHandler>
    where TDataEntry : struct
    where TResizeHandler : struct, IResizeHandler<TDataEntry, MultiIndex>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        SearchResult lastSearchResult,
        int candidateDataIndex)
    {
        PrivateAdd(hashTable, dataTable, handler, lastSearchResult, candidateDataIndex, MultiIndex.NoNext);
    }

    private static void PrivateAdd(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        SearchResult lastSearchResult,
        int candidateDataIndex,
        int candidateDataNext)
    {
        uint candidateReducedHashCode = lastSearchResult.ReducedHashCode;
        uint candidateDriftPlusOne = lastSearchResult.DriftPlusOne;
        MultiIndex candidateMultiIndex = new()
        {
            Previous = (int)candidateReducedHashCode,
            Next = candidateDataNext,
            IsSubsequent = false,
        };

        if (lastSearchResult.Case == SearchCase.EmptyEntryFound)
        {
            hashTable[candidateReducedHashCode] = new HashEntry
            {
                DriftPlusOne = candidateDriftPlusOne,
                ForwardIndex = candidateDataIndex,
            };
            handler.SetBackIndex(dataTable, candidateDataIndex, candidateMultiIndex);
            return;
        }
        
        // it is possible that the item is found, but we still need to plug the candidate
        if (lastSearchResult.Case == SearchCase.ItemFound)
        {
            // the existing first corresponding item is shifted inside the dataTable to become the second connected
            int shiftedDataIndex = lastSearchResult.ForwardIndex;
            var shiftedMultiIndex = handler.GetBackIndex(dataTable, shiftedDataIndex);
            
            candidateMultiIndex.Next = shiftedDataIndex;
            shiftedMultiIndex.IsSubsequent = true;
            shiftedMultiIndex.Previous = candidateDataIndex;
            handler.SetBackIndex(dataTable, candidateDataIndex, candidateMultiIndex);
            handler.SetBackIndex(dataTable, shiftedDataIndex, shiftedMultiIndex);
            hashTable[candidateReducedHashCode].ForwardIndex = candidateDataIndex;
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
                candidateMultiIndex.Previous = (int)candidateReducedHashCode;
                handler.SetBackIndex(dataTable, candidateDataIndex, candidateMultiIndex);
                return;
            }
            
            // we have drifted too long: we must swap the current data with the candidate data
            if (occupiedDriftPlusOne < candidateDriftPlusOne)
            {
                int forwardIndex = hashTable[candidateReducedHashCode].ForwardIndex;
                hashTable[candidateReducedHashCode].ForwardIndex = candidateDataIndex;
                hashTable[candidateReducedHashCode].DriftPlusOne = candidateDriftPlusOne;
                candidateMultiIndex.Previous = (int)candidateReducedHashCode;
                handler.SetBackIndex(dataTable, candidateDataIndex, candidateMultiIndex);
                
                candidateDataIndex = forwardIndex;
                candidateDriftPlusOne = occupiedDriftPlusOne;
                candidateMultiIndex = handler.GetBackIndex(dataTable, forwardIndex);
            }
            
            HashCodeReduction.MoveReducedHashCode(ref candidateReducedHashCode, hashTable.Length);
            candidateDriftPlusOne++;
        }
    }

    public static void InitialLastBackIndex(
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int newDataCount)
    {
        int lastDataIndex = newDataCount - 1;
        var multiIndex = new MultiIndex
        {
            IsSubsequent = false,
            Next = MultiIndex.NoNext,
            Previous = -1000, // will be reset by capacity change
        };
        handler.SetBackIndex(dataTable, lastDataIndex, multiIndex);
    }

    public static HashEntry[] ChangeCapacity(
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int newHashTableCapacity,
        int newDataCount)
    {
        var hashTable = new HashEntry[newHashTableCapacity];
        
        for (int i = 0; i < newDataCount; i++)
        {
            if (handler.GetBackIndex(dataTable, i).IsSubsequent)
                continue;
            var hashCode = handler.GetHashCodeAt(dataTable, i);
            var reducedHashCode = HashCodeReduction.ComputeReducedHashCode(hashCode, newHashTableCapacity);
            var next = handler.GetBackIndex(dataTable, i).Next;
            var searchResult = hashTable[reducedHashCode].DriftPlusOne == HashEntry.DriftForUnused ?
                SearchResult.CreateForEmptyEntry(reducedHashCode, HashEntry.Optimal) :
                SearchResult.CreateWhenSearchStopped(reducedHashCode, HashEntry.Optimal);
            PrivateAdd(hashTable, dataTable, handler, searchResult, i, next);
        }
        
        return hashTable;
    }
}