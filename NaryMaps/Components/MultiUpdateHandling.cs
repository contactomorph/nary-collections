using System.Diagnostics;
using NaryMaps.Primitives;

namespace NaryMaps.Components;

public static class MultiUpdateHandling<TDataEntry, TResizeHandler>
    where TDataEntry : struct
    where TResizeHandler : struct, IResizeHandler<TDataEntry, MultiIndex>
{
    public static void Add(
        ref HashEntry[] hashTable,
        ref int count,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        SearchResult lastSearchResult,
        int candidateDataIndex,
        int newDataCount)
    {
        // When this function is called, new data has already been added using
        // DataHandling<TDataTuple, THashTuple, TIndexTuple>.AddOnlyData. This implies that line
        // dataTable[newDataCount-1] already contains the correct data tuple and hash tuple.
        // This function is to be called for every possible handler and every corresponding hashTable. Its role is
        // to "connect" line dataTable[newDataCount-1] by modifying all data that must refer it.
        
        // We determine if connecting dataTable[newDataCount-1] either implies:
        // - just to connect it with the lines inside dataTable with the same key;
        // - to add a brand-new entry in hashTable because the key was not used so far.
        if (lastSearchResult.Case == SearchCase.ItemFound)
        {
            AddStrictly(hashTable, dataTable, handler, lastSearchResult, candidateDataIndex, MultiIndex.NoNext);
        }
        else
        {
            ++count;
        
            // In the second case, we either need:
            // - to recreate hashTable entirely in case it now contains too many data;
            // - just to add a new entry in hashTable and possibly to shift some existing entries.
            if (HashEntry.IsFullEnough(hashTable.Length, count))
            {
                // Initialize multi-index for the last data entry before resizing hashTable
                int lastDataIndex = newDataCount - 1;
                var multiIndex = new MultiIndex
                {
                    IsSubsequent = false,
                    Next = MultiIndex.NoNext,
                    Previous = -1000, // will be reset by capacity change
                };
                handler.SetBackIndex(dataTable, lastDataIndex, multiIndex);
                
                int newHashTableCapacity = HashEntry.IncreaseCapacity(hashTable.Length);
                
                hashTable = ChangeCapacity(dataTable, handler, newHashTableCapacity, newDataCount);
            }
            else
            {
                AddStrictly(hashTable, dataTable, handler, lastSearchResult, candidateDataIndex, MultiIndex.NoNext);
            }
        }
    }

    public static void AddStrictly(
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

    public static void Remove(
        ref HashEntry[] hashTable,
        ref int count,
        int currentDataCount,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int removedDataIndex)
    {
        // When this function is called, data has not been removed yet. This implies that
        // line dataTable[removedDataIndex] still contains everything needed.
        // Neither is this line modified inside this function. This function is to be called for every possible
        // handler and every corresponding hashTable before dataTable[removedDataIndex] is finally updated
        // by DataHandling<TDataTuple, THashTuple, TIndexTuple>.RemoveOnlyData. Its role is to "forget" about
        // line dataTable[removedDataIndex] by modifying all data that may refer it.
        
        MustBeStrictyPositive(count);
        MustBeSmallEnough(count, hashTable.Length);
        
        MultiIndex forgottenBackIndex = handler.GetBackIndex(dataTable, removedDataIndex);

        int newDataCount = currentDataCount - 1;

        // We determine if forgetting dataTable[removedDataIndex] either implies:
        // - to remove the indirection from hashTable (because the removed key is not used anywhere anymore);
        // - just to get around the line by reconnecting all the lines from dataTable that were connected so far.
        if (forgottenBackIndex is { IsSubsequent: false, Next: MultiIndex.NoNext })
        {
            int newCount = count - 1;
            
            // In the first case, we either need:
            // - to recreate hashTable entirely in case it does not contain enough data anymore;
            // - just to delete the indirection from hashTable.
            if (HashEntry.IsSparseEnough(hashTable.Length, newCount))
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
                RemoveStrictly(hashTable, dataTable, handler, forgottenBackIndex.Previous);
            }

            count = newCount;
        }
        else
        {
            Forget(hashTable, dataTable, handler, forgottenBackIndex);
        }

        // We have to prepare for the future removal of dataTable[removedDataIndex].
        // As dataTable must have continuous data, we will move the last line in dataTable (that is to say
        // datatable[newDataCount]) to replace dataTable[removedDataIndex]. Again this replacement is not done now.
        // However, we update hashTable and other lines of dataTable as if datatable[newDataCount] had already moved.
        Condense(hashTable, dataTable, handler, removedDataIndex, lastDataIndex: newDataCount);
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
        // and finally update all related indexes.
        
        var multiIndex = handler.GetBackIndex(dataTable, lastDataIndex);

        if (multiIndex.IsSubsequent)
        {
            var previous = handler.GetBackIndex(dataTable, multiIndex.Previous) with
                { Next = removedDataIndex };
            handler.SetBackIndex(dataTable, multiIndex.Previous, previous);
        }
        else
        {
            hashTable[multiIndex.Previous].ForwardIndex = removedDataIndex;
        }

        if (multiIndex.Next != MultiIndex.NoNext)
        {
            var next = handler.GetBackIndex(dataTable, multiIndex.Next) with { Previous = removedDataIndex };
            handler.SetBackIndex(dataTable, multiIndex.Next, next);
        }
    }

    private static void RemoveStrictly(
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
            var multiIndex = handler.GetBackIndex(dataTable, forwardIndex) with { Previous = (int)reducedHashCode };
            handler.SetBackIndex(dataTable, forwardIndex, multiIndex);
        
            reducedHashCode = nextReducedHashCode;
            HashCodeReduction.MoveReducedHashCode(ref nextReducedHashCode, hashTable.Length);
        }
    }

    private static void Forget(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        TResizeHandler handler,
        MultiIndex forgottenBackIndex)
    {
        if (forgottenBackIndex.IsSubsequent)
        {
            var previous = handler.GetBackIndex(dataTable, forgottenBackIndex.Previous) with
                { Next = forgottenBackIndex.Next };
            handler.SetBackIndex(dataTable, forgottenBackIndex.Previous, previous);
            
            if (forgottenBackIndex.Next != MultiIndex.NoNext)
            {
                var next = handler.GetBackIndex(dataTable, forgottenBackIndex.Next) with
                    { Previous = forgottenBackIndex.Previous };
                handler.SetBackIndex(dataTable, forgottenBackIndex.Next, next);
            }
        }
        else
        {
            MustNotBeLast(forgottenBackIndex);
            
            var next = handler.GetBackIndex(dataTable, forgottenBackIndex.Next) with
                { Previous = forgottenBackIndex.Previous, IsSubsequent = false };
            handler.SetBackIndex(dataTable, forgottenBackIndex.Next, next);
            hashTable[forgottenBackIndex.Previous].ForwardIndex = forgottenBackIndex.Next;
        }
    }

    public static HashEntry[] ChangeCapacity(
        TDataEntry[] dataTable,
        TResizeHandler handler,
        int newHashTableCapacity,
        int dataCountToConsider,
        int except = -1)
    {
        var hashTable = new HashEntry[newHashTableCapacity];
        
        for (int i = 0; i < dataCountToConsider; i++)
        {
            if (i == except || handler.GetBackIndex(dataTable, i).IsSubsequent)
                continue;
            var hashCode = handler.GetHashCodeAt(dataTable, i);
            var reducedHashCode = HashCodeReduction.ComputeReducedHashCode(hashCode, newHashTableCapacity);
            var next = handler.GetBackIndex(dataTable, i).Next;
            var searchResult = hashTable[reducedHashCode].DriftPlusOne == HashEntry.DriftForUnused ?
                SearchResult.CreateForEmptyEntry(reducedHashCode, HashEntry.Optimal) :
                SearchResult.CreateWhenSearchStopped(reducedHashCode, HashEntry.Optimal);
            AddStrictly(hashTable, dataTable, handler, searchResult, i, next);
        }
        
        return hashTable;
    }
    
    [Conditional("DEBUG")]
    private static void MustNotBeLast(MultiIndex multiIndex)
    {
        Debug.Assert(multiIndex.Next != MultiIndex.NoNext, "Provided multiIndex should not be the last one");
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