using System.Collections;
using System.Runtime.CompilerServices;

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

public static class TableHandling<TDataTuple, THashTuple, TIndexTuple>
    where TDataTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
{
    public static int AddOnlyData(
        ref DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        TDataTuple dataTuple,
        THashTuple hashTuple,
        ref int dataCount)
    {
        if (dataCount == dataTable.Length)
            Array.Resize(ref dataTable, dataTable.Length << 1);
        int dataIndex = dataCount;
        ++dataCount;
        
        dataTable[dataIndex] = new DataEntry<TDataTuple, THashTuple, TIndexTuple>
        {
            DataTuple = dataTuple,
            HashTuple = hashTuple,
        };
        return dataIndex;
    }
}

internal static class TableHandling<TDataEntry, T> where TDataEntry : struct
{
    public static TableHandling.SearchResult ContainsForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IDataProjector<TDataEntry, T> projector,
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
        uint driftPlusOne = HashEntry.Optimal;
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

    public static void AddForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IDataProjector<TDataEntry, T> projector,
        TableHandling.SearchResult lastSearchResult,
        int candidateDataIndex)
    {
        uint candidateReducedHashCode = (uint)lastSearchResult.HashIndex;
        uint candidateDriftPlusOne = lastSearchResult.DriftPlusOne;

        if (lastSearchResult.Case == TableHandling.SearchCase.EmptyEntryFound)
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
    
    public static void RemoveOnlyData(
        ref TDataEntry[] dataTable,
        int dataIndex,
        ref int dataCount)
    {
        --dataCount;
        if (dataIndex == dataCount)
        {
            dataTable[dataIndex] = default;
        }
        else
        {
            dataTable[dataIndex] = dataTable[dataCount];
            dataTable[dataCount] = default;
        }

        if (dataCount < dataTable.Length >> 2 && DataEntry.TableMinimalLength < dataTable.Length)
            Array.Resize(ref dataTable, Math.Max(dataTable.Length >> 1, DataEntry.TableMinimalLength));
    }
    
    public static void RemoveForUnique(
        HashEntry[] hashTable,
        TDataEntry[] dataTable,
        IDataProjector<TDataEntry, T> projector,
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
}