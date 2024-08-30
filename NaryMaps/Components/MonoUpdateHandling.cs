using System.Diagnostics;
using NaryMaps.Primitives;

namespace NaryMaps.Components;

public static class MonoUpdateHandling<TDataEntry, TResizeHandler>
    where TDataEntry : struct
    where TResizeHandler : struct, IResizeHandler<TDataEntry, int>
{
    public static void Add(
        ref HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        SearchResult lastSearchResult,
        int candidateDataIndex,
        int newDataCount)
    {
        MustNotBeFound(lastSearchResult);
        
        if (HashEntry.IsFullEnough(hashTable.Length, newDataCount))
        {
            int newHashTableCapacity = HashEntry.IncreaseCapacity(hashTable.Length);
            hashTable = ChangeCapacity(dataTable, handler, newHashTableCapacity, newDataCount);
        }
        else
        {
            AddStrictly(hashTable, dataTable, handler, lastSearchResult, candidateDataIndex);
        }
    }
    
    public static void AddStrictly(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        SearchResult lastSearchResult,
        int candidateDataIndex)
    {
        uint candidateReducedHashCode = lastSearchResult.ReducedHashCode;
        uint candidateDriftPlusOne = lastSearchResult.DriftPlusOne;

        if (lastSearchResult.Case == SearchCase.EmptyEntryFound)
        {
            hashTable[candidateReducedHashCode] = new HashEntry
            {
                DriftPlusOne = candidateDriftPlusOne,
                ForwardIndex = candidateDataIndex,
            };
            handler.SetBackIndex(dataTable, candidateDataIndex, (int)candidateReducedHashCode);
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
                handler.SetBackIndex(dataTable, candidateDataIndex, (int)candidateReducedHashCode);
                return;
            }
            
            // we have drifted too long: we must swap the current data with the candidate data
            if (occupiedDriftPlusOne < candidateDriftPlusOne)
            {
                int forwardIndex = hashTable[candidateReducedHashCode].ForwardIndex;
                hashTable[candidateReducedHashCode].ForwardIndex = candidateDataIndex;
                hashTable[candidateReducedHashCode].DriftPlusOne = candidateDriftPlusOne;
                handler.SetBackIndex(dataTable, candidateDataIndex, (int)candidateReducedHashCode);

                candidateDataIndex = forwardIndex;
                candidateDriftPlusOne = occupiedDriftPlusOne;
            }
            
            HashCodeReduction.MoveReducedHashCode(ref candidateReducedHashCode, hashTable.Length);
            candidateDriftPlusOne++;
        }
    }
    
    public static void Remove(
        ref HashEntry[] hashTable,
        int currentDataCount,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int removedDataIndex)
    {
        // When this function is called, data has not been removed yet. This implies that
        // line dataTable[removedDataIndex] still contains everything needed.
        // We may update hashTable even if it gets resized after.
        
        MustBeStrictyPositive(currentDataCount);
        MustBeSmallEnough(currentDataCount, hashTable.Length);
        
        int forgottenBackIndex = handler.GetBackIndex(dataTable, removedDataIndex);
        
        int newDataCount = currentDataCount - 1;
        
        if (HashEntry.IsSparseEnough(hashTable.Length, newDataCount))
        {
            int newHashTableCapacity = HashEntry.DecreaseCapacity(hashTable.Length);
            hashTable = ChangeCapacity(
                dataTable,
                handler,
                newHashTableCapacity,
                currentDataCount,
                except: removedDataIndex);
        }
        else
        {
            RemoveStrictly(hashTable, dataTable, handler, forgottenBackIndex);
        }
        Condense(hashTable, dataTable, handler, removedDataIndex, lastDataIndex: newDataCount);
    }

    public static void RemoveStrictly(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int dataIndex)
    {
        uint reducedHashCode = (uint)dataIndex;
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
            handler.SetBackIndex(dataTable, forwardIndex, (int)reducedHashCode);

            reducedHashCode = nextReducedHashCode;
            HashCodeReduction.MoveReducedHashCode(ref nextReducedHashCode, hashTable.Length);
        }
    }

    private static void Condense(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int removedDataIndex,
        int lastDataIndex)
    {
        if (removedDataIndex == lastDataIndex) return;
        
        // If forgottenBackIndex is not the last item in dataTable,
        // this last item will be moved in dataTable from last position to removedDataIndex.
        // We must look for the back index of this last item, then find the corresponding entry in hashTable
        // and finally update the HashEntry.ForwardIndex to removedDataIndex.
        
        var backIndex = handler.GetBackIndex(dataTable, lastDataIndex);
        hashTable[backIndex].ForwardIndex = removedDataIndex;
    }

    public static HashEntry[] ChangeCapacity(
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int newHashTableCapacity,
        int newDataCount,
        int except = -1)
    {
        var hashTable = new HashEntry[newHashTableCapacity];
        
        for (int i = 0; i < newDataCount; i++)
        {
            if (i == except)
                continue;
            var hashCode = handler.GetHashCodeAt(dataTable, i);
            var reducedHashCode = HashCodeReduction.ComputeReducedHashCode(hashCode, newHashTableCapacity);
            var searchResult = hashTable[reducedHashCode].DriftPlusOne == HashEntry.DriftForUnused ?
                SearchResult.CreateForEmptyEntry(reducedHashCode, HashEntry.Optimal) :
                SearchResult.CreateWhenSearchStopped(reducedHashCode, HashEntry.Optimal);
            AddStrictly(hashTable, dataTable, handler, searchResult, i);
        }

        return hashTable;
    }
    
    [Conditional("DEBUG")]
    private static void MustNotBeFound(SearchResult result)
    {
        Debug.Assert(result.Case != SearchCase.ItemFound, "New item cannot have been found when adding it");
    }
    
    [Conditional("DEBUG")]
    private static void MustBeSmallEnough(int count, int capacity)
    {
        Debug.Assert(count <= capacity, "Count be smaller than capacity");
    }
    
    [Conditional("DEBUG")]
    private static void MustBeStrictyPositive(int count)
    {
        Debug.Assert(0 < count, "Count should be strictly positive");
    }
}