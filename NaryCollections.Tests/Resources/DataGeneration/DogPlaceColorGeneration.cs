using NaryCollections.Details;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorTuple = (Types.Dog Dog, string Place, System.Drawing.Color Color);
using HashTuple = (uint, uint, uint);
using IndexTuple = ValueTuple<int>;

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
            var hashTuple = ToHashCodes(tuple);
            
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
        Func<HashTuple, uint> hashProj,
        Func<DogPlaceColorTuple, object> dataProj)
    {
        int size = data.Count * 3 / 2;
        
        hashTable = new HashEntry[size];
        dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[size];

        var tupleSet = new HashSet<DogPlaceColorTuple>();
        var itemSet = new HashSet<object>();
        
        int i = 0;
        foreach (var tuple in data)
        {
            var hashTuple = ToHashCodes(tuple);
            
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
                hashProj(hashTuple),
                i);

            ++i;
        }
    }
    
    private static void UpdateHashTable(
        HashEntry[] hashTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        uint newItemProjectedHashCode,
        int newItemDataIndex)
    {
        uint reducedHashTuple = TableHandling.ComputeReducedHashCode(newItemProjectedHashCode, hashTable.Length);
        uint driftPlusOne = 1;
            
        while (true)
        {
            uint currentDriftPlusOne = hashTable[reducedHashTuple].DriftPlusOne;
                
            if (currentDriftPlusOne == HashEntry.DriftForUnused)
            {
                hashTable[reducedHashTuple] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item1 = (int)reducedHashTuple;
                break;
            }

            if (driftPlusOne < currentDriftPlusOne)
            {
                TableHandling.MoveReducedHashCode(ref reducedHashTuple, hashTable.Length);
                driftPlusOne++;
            }
            else
            {
                int replacementDataIndex = hashTable[reducedHashTuple].ForwardIndex;

                hashTable[reducedHashTuple] = new()
                {
                    DriftPlusOne = driftPlusOne,
                    ForwardIndex = newItemDataIndex,
                };
                dataTable[newItemDataIndex].BackIndexesTuple.Item1 = (int)reducedHashTuple;
                
                TableHandling.MoveReducedHashCode(ref reducedHashTuple, hashTable.Length);
                driftPlusOne = currentDriftPlusOne + 1;
                newItemDataIndex = replacementDataIndex;
            }
        }
    }

    public static void CheckTablesConsistencyForUnique(
        HashEntry[] hashTable,
        DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[] dataTable,
        int dataLength)
    {
        for (int i = 0; i < dataLength; i++)
        {
            var hashTuple = ToHashCodes(dataTable[i].DataTuple);
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

    public static (uint, uint, uint) ToHashCodes(DogPlaceColorTuple tuple)
    {
        var a = (uint)tuple.Dog.GetHashCode();
        var b = (uint)tuple.Place.GetHashCode();
        var c = (uint)tuple.Color.GetHashCode();
        return (a, b, c);
    }

    private static Exception CreateConsistencyError(string firstPlace, string secondPlace)
    {
        return new InvalidDataException($"{firstPlace} and {secondPlace} are inconsistent");
    }
}