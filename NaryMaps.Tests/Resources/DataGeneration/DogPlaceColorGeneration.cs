using NaryMaps.Primitives;

namespace NaryMaps.Tests.Resources.DataGeneration;

using DogPlaceColorTuple = (Types.Dog Dog, string Place, System.Drawing.Color Color);
using HashTuple = (uint, uint, uint);
using IndexTuple = (int, MultiIndex);

internal static class DogPlaceColorGeneration
{
    public static void CreateDataTableOnly(
        IReadOnlyCollection<DogPlaceColorTuple> data,
        out DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable)
    {
        int size = data.Count * 3 / 2;
        dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[size];
        
        var tupleSet = new HashSet<DogPlaceColorTuple>();
        
        int i = 0;
        foreach (var tuple in data)
        {
            var hashTuple = DogPlaceColorProjector.GetHashTupleComputer()(tuple);
            
            dataTable[i] = new()
            {
                DataTuple = tuple,
                HashTuple = hashTuple,
                BackIndexesTuple = (-1000, default),
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
        Func<HashTuple, uint> hashProj,
        Func<DogPlaceColorTuple, object> dataProj,
        Func<DogPlaceColorTuple, (uint, uint, uint)>? hashTupleComputer = null)
    {
        hashTupleComputer ??= DogPlaceColorProjector.GetHashTupleComputer();
        int size = data.Count * 3 / 2;
        
        hashTable = new HashEntry[size];
        dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[size];

        var tupleSet = new HashSet<DogPlaceColorTuple>();
        var itemSet = new HashSet<object>();
        
        int i = 0;
        foreach (var tuple in data)
        {
            var hashTuple = hashTupleComputer(tuple);
            
            dataTable[i] = new()
            {
                DataTuple = tuple,
                HashTuple = hashTuple,
                BackIndexesTuple = (-1000, default),
            };

            if (!tupleSet.Add(tuple))
                throw new InvalidDataException("Duplicate line");
            if(!itemSet.Add(dataProj(tuple)))
                throw new InvalidDataException("Duplicate participant");
            
            UpdateHashTable(
                hashTable,
                dataTable, 
                hashProj(hashTuple),
                i);

            ++i;
        }
    }
    
    private static void UpdateHashTable(
        HashEntry[] hashTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        uint newItemHashCode,
        int newItemDataIndex)
    {
        uint driftPlusOne = HashEntry.Optimal;
        uint newItemReducedHashCode = HashCodeReduction.ComputeReducedHashCode(newItemHashCode, hashTable.Length);
        
        while (true)
        {
            uint candidateDriftPlusOne = hashTable[newItemReducedHashCode].DriftPlusOne;
                
            if (candidateDriftPlusOne == HashEntry.DriftForUnused)
            {
                hashTable[newItemReducedHashCode] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item1 = (int)newItemReducedHashCode;
                break;
            }

            if (driftPlusOne <= candidateDriftPlusOne)
            {
                HashCodeReduction.MoveReducedHashCode(ref newItemReducedHashCode, hashTable.Length);
                driftPlusOne++;
            }
            else
            {
                int replacementDataIndex = hashTable[newItemReducedHashCode].ForwardIndex;

                hashTable[newItemReducedHashCode] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item1 = (int)newItemReducedHashCode;
                
                HashCodeReduction.MoveReducedHashCode(ref newItemReducedHashCode, hashTable.Length);
                driftPlusOne = candidateDriftPlusOne + 1;
                newItemDataIndex = replacementDataIndex;
            }
        }
    }

    public static int CreateTablesForNonUnique(
        IReadOnlyCollection<DogPlaceColorTuple> data,
        out HashEntry[] hashTable,
        out DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        Func<HashTuple, uint> hashProj,
        Func<DogPlaceColorTuple, object> dataProj,
        Func<DogPlaceColorTuple, (uint, uint, uint)>? hashTupleComputer = null,
        bool makeHashTableSmaller = true)
    {
        hashTupleComputer ??= DogPlaceColorProjector.GetHashTupleComputer();
        int hashTableSize = makeHashTableSmaller ? data.Count * 4 / 5 : data.Count;
        int dataTableSize = data.Count * 3 / 2;
        int hashEntryCount = 0;
        
        hashTable = new HashEntry[hashTableSize];
        dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[dataTableSize];
        
        var tupleSet = new HashSet<DogPlaceColorTuple>();
        
        int i = 0;
        foreach (var tuple in data)
        {
            if (hashEntryCount == hashTable.Length)
                throw new InvalidOperationException("Hash table is full");
            
            var hashTuple = hashTupleComputer(tuple);
            
            MultiIndex multiIndex = new()
            {
                IsSubsequent = false,
                Previous = -1, // to be replaced
                Next = MultiIndex.NoNext,
            };
            dataTable[i] = new()
            {
                DataTuple = tuple,
                HashTuple = hashTuple,
                BackIndexesTuple = (-1000, multiIndex),
            };
    
            if (!tupleSet.Add(tuple))
                throw new InvalidDataException("Duplicate line");
    
            uint newItemHashCode = hashProj(hashTuple);
            object newItemProjectedValue = dataProj(tuple);
    
            bool IsProjectionCorresponding(HashTuple ht, DogPlaceColorTuple dt)
            {
                uint existingItemHashCode = hashProj(ht);
                object existingItemProjectedValue = dataProj(dt);
                return existingItemHashCode == newItemHashCode
                       && existingItemProjectedValue.Equals(newItemProjectedValue);
            }
    
            bool hashEntryAdded = UpdateHashTable(
                hashTable,
                dataTable,
                newItemHashCode,
                newItemDataIndex: i,
                IsProjectionCorresponding);

            if (hashEntryAdded)
                ++hashEntryCount;
            
            ++i;
        }

        return hashEntryCount;
    }
    
    private static bool UpdateHashTable(
        HashEntry[] hashTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        uint newItemHashCode,
        int newItemDataIndex,
        Func<HashTuple, DogPlaceColorTuple, bool> isProjectionCorresponding)
    {
        uint driftPlusOne = HashEntry.Optimal;
        uint newItemReducedHashCode = HashCodeReduction.ComputeReducedHashCode(newItemHashCode, hashTable.Length);
            
        while (true)
        {
            uint candidateDriftPlusOne = hashTable[newItemReducedHashCode].DriftPlusOne;
                
            if (candidateDriftPlusOne == HashEntry.DriftForUnused)
            {
                hashTable[newItemReducedHashCode] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item2.Previous = (int)newItemReducedHashCode;
                
                return true;
            }
    
            int dataIndex = hashTable[newItemReducedHashCode].ForwardIndex;
    
            if (isProjectionCorresponding(dataTable[dataIndex].HashTuple, dataTable[dataIndex].DataTuple))
            {
                dataTable[newItemDataIndex].BackIndexesTuple.Item2.Previous = (int)newItemReducedHashCode;
                dataTable[newItemDataIndex].BackIndexesTuple.Item2.Next = dataIndex;
                dataTable[dataIndex].BackIndexesTuple.Item2.IsSubsequent = true;
                dataTable[dataIndex].BackIndexesTuple.Item2.Previous = newItemDataIndex;
                hashTable[newItemReducedHashCode].ForwardIndex = newItemDataIndex;

                return false;
            }
    
            if (driftPlusOne <= candidateDriftPlusOne)
            {
                HashCodeReduction.MoveReducedHashCode(ref newItemReducedHashCode, hashTable.Length);
                driftPlusOne++;
            }
            else
            {
                int replacementCorrespondenceIndex = hashTable[newItemReducedHashCode].ForwardIndex;
                
                hashTable[newItemReducedHashCode] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item2.Previous = (int)newItemReducedHashCode;
                
                HashCodeReduction.MoveReducedHashCode(ref newItemReducedHashCode, hashTable.Length);
                driftPlusOne = candidateDriftPlusOne + 1;
                newItemDataIndex = replacementCorrespondenceIndex;
            }
        }
    }
}