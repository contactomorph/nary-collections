using NaryCollections.Details;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorTuple = (Types.Dog Dog, string Place, System.Drawing.Color Color);
using HashTuple = (uint, uint, uint);
using IndexTuple = ValueTuple<int>;

internal static class DogPlaceColorGeneration
{
    public static void CreateDataTableOnly(
        IReadOnlyCollection<DogPlaceColorTuple> data,
        out DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        DogPlaceColorProjector? projector = null)
    {
        projector ??= DogPlaceColorProjector.Instance;
        int size = data.Count * 3 / 2;
        dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[size];
        
        var tupleSet = new HashSet<DogPlaceColorTuple>();
        
        int i = 0;
        foreach (var tuple in data)
        {
            var hashTuple = projector.ComputeHashTuple(tuple);;
            
            dataTable[i] = new()
            {
                DataTuple = tuple,
                HashTuple = hashTuple,
                BackIndexesTuple = ValueTuple.Create(-1000),
            };

            if (!tupleSet.Add(tuple))
                throw new InvalidDataException("Duplicate line");

            ++i;
        }
    }
    
    public static void CreateTablesForUnique(
        IReadOnlyCollection<DogPlaceColorTuple> data,
        out HashEntry[] hashTable,
        out DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        Func<HashTuple, int, uint> reducedHashProj,
        Func<DogPlaceColorTuple, object> dataProj,
        DogPlaceColorProjector? projector = null)
    {
        projector ??= DogPlaceColorProjector.Instance;
        int size = data.Count * 3 / 2;
        
        hashTable = new HashEntry[size];
        dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[size];

        var tupleSet = new HashSet<DogPlaceColorTuple>();
        var itemSet = new HashSet<object>();
        
        int i = 0;
        foreach (var tuple in data)
        {
            var hashTuple = projector.ComputeHashTuple(tuple);
            
            dataTable[i] = new()
            {
                DataTuple = tuple,
                HashTuple = hashTuple,
                BackIndexesTuple = ValueTuple.Create(-1000),
            };

            if (!tupleSet.Add(tuple))
                throw new InvalidDataException("Duplicate line");
            if(!itemSet.Add(dataProj(tuple)))
                throw new InvalidDataException("Duplicate participant");
            
            UpdateHashTable(
                hashTable,
                dataTable, 
                reducedHashProj(hashTuple, hashTable.Length),
                i);

            ++i;
        }
    }
    
    private static void UpdateHashTable(
        HashEntry[] hashTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        uint newItemReducedHashTuple,
        int newItemDataIndex)
    {
        uint driftPlusOne = 1;
            
        while (true)
        {
            uint candidateDriftPlusOne = hashTable[newItemReducedHashTuple].DriftPlusOne;
                
            if (candidateDriftPlusOne == HashEntry.DriftForUnused)
            {
                hashTable[newItemReducedHashTuple] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item1 = (int)newItemReducedHashTuple;
                break;
            }

            if (driftPlusOne <= candidateDriftPlusOne)
            {
                TableHandling.MoveReducedHashCode(ref newItemReducedHashTuple, hashTable.Length);
                driftPlusOne++;
            }
            else
            {
                int replacementDataIndex = hashTable[newItemReducedHashTuple].ForwardIndex;

                hashTable[newItemReducedHashTuple] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item1 = (int)newItemReducedHashTuple;
                
                TableHandling.MoveReducedHashCode(ref newItemReducedHashTuple, hashTable.Length);
                driftPlusOne = candidateDriftPlusOne + 1;
                newItemDataIndex = replacementDataIndex;
            }
        }
    }

    public static void CreateTablesForNonUnique(
        IReadOnlyCollection<DogPlaceColorTuple> data,
        out HashEntry[] hashTable,
        out CorrespondenceEntry[] correspondenceTable,
        out DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        Func<HashTuple, int, uint> reducedHashProj,
        Func<DogPlaceColorTuple, object> dataProj,
        DogPlaceColorProjector? projector = null)
    {
        projector ??= DogPlaceColorProjector.Instance;
        int size = data.Count  * 2 / 2;
        
        hashTable = new HashEntry[size];
        correspondenceTable = new CorrespondenceEntry[size];
        dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[size];
        
        var tupleSet = new HashSet<DogPlaceColorTuple>();
        
        int i = 0;
        foreach (var tuple in data)
        {
            var hashTuple = projector.ComputeHashTuple(tuple);
            
            correspondenceTable[i] = new()
            {
                Status = EntryStatus.First,
                DataIndex = i,
                Previous = -1, // to be replaced
                Next = CorrespondenceEntry.NoNextCorrespondence,
            };
            dataTable[i] = new()
            {
                DataTuple = tuple,
                HashTuple = hashTuple,
                BackIndexesTuple = ValueTuple.Create(i),
            };

            if (!tupleSet.Add(tuple))
                throw new InvalidDataException("Duplicate line");

            uint newItemReducedHashTuple = reducedHashProj(hashTuple, size);
            object newItemProjectedValue = dataProj(tuple);

            bool IsProjectionCorresponding(HashTuple ht, DogPlaceColorTuple dt)
            {
                uint existingItemReducedHashTuple = reducedHashProj(ht, size);
                object existingItemProjectedValue = dataProj(dt);
                return existingItemReducedHashTuple == newItemReducedHashTuple
                       && existingItemProjectedValue.Equals(newItemProjectedValue);
            }

            UpdateHashTable(
                hashTable,
                correspondenceTable,
                dataTable,
                newItemReducedHashTuple,
                newItemCorrespondenceIndex: i,
                IsProjectionCorresponding);
            
            ++i;
        }
    }
    
    private static void UpdateHashTable(
        HashEntry[] hashTable,
        CorrespondenceEntry[] correspondenceTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        uint newItemReducedHashTuple,
        int newItemCorrespondenceIndex,
        Func<HashTuple, DogPlaceColorTuple, bool> isProjectionCorresponding)
    {
        uint driftPlusOne = 1;
            
        while (true)
        {
            uint candidateDriftPlusOne = hashTable[newItemReducedHashTuple].DriftPlusOne;
                
            if (candidateDriftPlusOne == HashEntry.DriftForUnused)
            {
                hashTable[newItemReducedHashTuple] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemCorrespondenceIndex,
                };
                correspondenceTable[newItemCorrespondenceIndex].Previous = (int)newItemReducedHashTuple;
                
                break;
            }

            int forwardIndex = hashTable[newItemReducedHashTuple].ForwardIndex;
            int dataIndex = correspondenceTable[forwardIndex].DataIndex;

            if (isProjectionCorresponding(dataTable[dataIndex].HashTuple, dataTable[dataIndex].DataTuple))
            {
                correspondenceTable[newItemCorrespondenceIndex].Previous = (int)newItemReducedHashTuple;
                correspondenceTable[newItemCorrespondenceIndex].Next = forwardIndex;
                correspondenceTable[forwardIndex].Status = EntryStatus.Subsequent;
                correspondenceTable[forwardIndex].Previous = newItemCorrespondenceIndex;
                hashTable[newItemReducedHashTuple].ForwardIndex = newItemCorrespondenceIndex;
                break;
            }

            if (driftPlusOne <= candidateDriftPlusOne)
            {
                TableHandling.MoveReducedHashCode(ref newItemReducedHashTuple, hashTable.Length);
                driftPlusOne++;
            }
            else
            {
                int replacementCorrespondenceIndex = hashTable[newItemReducedHashTuple].ForwardIndex;
                
                hashTable[newItemReducedHashTuple] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemCorrespondenceIndex,
                };
                correspondenceTable[newItemCorrespondenceIndex].Previous = (int)newItemReducedHashTuple;
                
                TableHandling.MoveReducedHashCode(ref newItemReducedHashTuple, hashTable.Length);
                driftPlusOne = candidateDriftPlusOne + 1;
                newItemCorrespondenceIndex = replacementCorrespondenceIndex;
            }
        }
    }

    public static void CheckTablesConsistencyForUnique(
        HashEntry[] hashTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        int dataLength,
        DogPlaceColorProjector? projector = null)
    {
        projector ??= DogPlaceColorProjector.Instance;
        
        for (int i = 0; i < dataLength; i++)
        {
            var hashTuple = projector.ComputeHashTuple(dataTable[i].DataTuple);
            if (hashTuple != dataTable[i].HashTuple)
                throw new InvalidDataException("Hash tuple is incorrect");
            int backIndex = dataTable[i].BackIndexesTuple.Item1;
            if (i != hashTable[backIndex].ForwardIndex)
                throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable[].BackIndexesTuple");
        }

        for (int i = 0; i < hashTable.Length; i++)
        {
            if (hashTable[i].DriftPlusOne != HashEntry.DriftForUnused)
            {
                int forwardIndex = hashTable[i].ForwardIndex;
                if (i != dataTable[forwardIndex].BackIndexesTuple.Item1)
                    throw CreateConsistencyError("hashTable[].ForwardIndex", "dataTable[].BackIndexesTuple");
            }
        }
    }

    public static void CheckTablesConsistencyForNonUnique(
        HashEntry[] hashTable,
        CorrespondenceEntry[] correspondenceTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        int dataLength,
        DogPlaceColorProjector? projector = null)
    {
        projector ??= DogPlaceColorProjector.Instance;
        
        for (int i = 0; i < dataLength; i++)
        {
            var hashTuple = projector.ComputeHashTuple(dataTable[i].DataTuple);
            if (hashTuple != dataTable[i].HashTuple)
                throw new InvalidDataException("Hash tuple is incorrect");
            int correspondenceIndex = dataTable[i].BackIndexesTuple.Item1;
            if (i != correspondenceTable[correspondenceIndex].DataIndex)
                throw CreateConsistencyError("correspondenceEntries[].DataIndex", "dataTable[].BackIndexesTuple");
            switch (correspondenceTable[correspondenceIndex].Status)
            {
                case EntryStatus.First:
                    int hashIndex = correspondenceTable[correspondenceIndex].Previous;
                    if (correspondenceIndex != hashTable[hashIndex].ForwardIndex)
                        throw CreateConsistencyError("hashTable[].ForwardIndex", "correspondenceTable[].Previous");
                    int next1 = correspondenceTable[correspondenceIndex].Next;
                    if (next1 != CorrespondenceEntry.NoNextCorrespondence && correspondenceTable[next1].Status != EntryStatus.Subsequent)
                        throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceTable[].Status");
                    break;
                case EntryStatus.Subsequent:
                    int previous = correspondenceTable[correspondenceIndex].Previous;
                    if (correspondenceIndex != correspondenceTable[previous].Next)
                        throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceTable[].Previous");
                    int next2 = correspondenceTable[correspondenceIndex].Next;
                    if (next2 != CorrespondenceEntry.NoNextCorrespondence && correspondenceTable[next2].Status != EntryStatus.Subsequent)
                        throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceTable[].Status");
                    break;
                default:
                    throw new InvalidDataException("Invalid empty correspondence");
            }
        }

        var verifiedIndex = new HashSet<int>();
        for (int i = 0; i < correspondenceTable.Length; i++)
        {
            switch (correspondenceTable[i].Status)
            {
                case EntryStatus.Unused:
                    if (!verifiedIndex.Add(i))
                        throw new InvalidDataException("Known index");
                    break;
                case EntryStatus.First:
                    if (!verifiedIndex.Add(i))
                        throw new InvalidDataException("Known index");
                    int previousJ = i;
                    int currentJ = correspondenceTable[i].Next;
                    while (currentJ != CorrespondenceEntry.NoNextCorrespondence)
                    {
                        if (!verifiedIndex.Add(currentJ))
                            throw new InvalidDataException("Known index");
                        if (correspondenceTable[currentJ].Status != EntryStatus.Subsequent)
                            throw new InvalidDataException("Invalid correspondence status");
                        if (correspondenceTable[currentJ].Previous != previousJ)
                            throw CreateConsistencyError("correspondenceTable[].Next", "correspondenceEntries[].Previous");
                        previousJ = currentJ;
                        currentJ = correspondenceTable[currentJ].Next;
                    }
                    break;
            }
        }

        for (int i = 0; i < hashTable.Length; i++)
        {
            if (hashTable[i].DriftPlusOne != HashEntry.DriftForUnused)
            {
                int forwardIndex = hashTable[i].ForwardIndex;
                if (i != correspondenceTable[forwardIndex].Previous)
                    throw CreateConsistencyError("hashTable[].ForwardIndex", "correspondenceEntries[].Previous");
            }
        }
    }

    private static Exception CreateConsistencyError(string firstPlace, string secondPlace)
    {
        return new InvalidDataException($"{firstPlace} and {secondPlace} are inconsistent");
    }
}