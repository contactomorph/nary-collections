using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

public class TableHandlingTests
{
    [Test]
    public void CheckItemExistenceForUniqueParticipantTest()
    {
        DogPlaceColorGeneration.CreateTablesForUnique(
            DogPlaceColorTuples.DataWithUniqueDogs,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog);
        
        var projector = DogProjector.Instance;

        foreach (var (dog, _, _) in DogPlaceColorTuples.DataWithUniqueDogs)
        {
            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.EqualTo(TableHandling.SearchCase.ItemFound));
        }
        
        foreach (var dog in Dogs.UnknownDogs)
        {
            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.Not.EqualTo(TableHandling.SearchCase.ItemFound));
        }

        DogPlaceColorGeneration.CheckTablesConsistencyForUnique(
            hashTable,
            dataTable,
            DogPlaceColorTuples.DataWithUniqueDogs.Count);
    }
    
    [Test]
    public void CheckItemExistenceForCompleteDataTupleTest()
    {
        DogPlaceColorGeneration.CreateTablesForUnique(
            DogPlaceColorTuples.Data,
            out var hashTable,
            out var dataTable,
            hashTuple => (uint)hashTuple.GetHashCode(),
            dataTuple => dataTuple);
        
        var projector = DogPlaceColorProjector.Instance;

        foreach (var tuple in DogPlaceColorTuples.Data)
        {
            var result = TableHandling<DogPlaceColorEntry, DogPlaceColorTuple>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)DogPlaceColorProjector.Instance.ComputeHashTuple(tuple).GetHashCode(),
                tuple);
        
            Assert.That(result.Case, Is.EqualTo(TableHandling.SearchCase.ItemFound));
        }

        DogPlaceColorGeneration.CheckTablesConsistencyForUnique(hashTable, dataTable, DogPlaceColorTuples.Data.Count);
    }

    [Test]
    public void CheckItemExistenceForNonUniqueParticipantTest()
    {
        DogPlaceColorGeneration.CreateTablesForNonUnique(
            DogPlaceColorTuples.Data,
            out var hashTable,
            out var correspondenceTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog);
        
        var projector = DogProjector.Instance;

        foreach (var dog in Dogs.KnownDogs)
        {
            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForNonUnique(
                hashTable,
                correspondenceTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.EqualTo(TableHandling.SearchCase.ItemFound));
        }
        
        foreach (var dog in Dogs.UnknownDogs)
        {
            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.Not.EqualTo(TableHandling.SearchCase.ItemFound));
        }

        DogPlaceColorGeneration.CheckTablesConsistencyForNonUnique(
            hashTable,
            correspondenceTable,
            dataTable,
            Dogs.KnownDogs.Count);
    }

    [Test]
    public void CheckItemAdditionForUniqueParticipantTest()
    {
        var data = Dogs.KnownDogsWithHashCode
            .Select(dh => (dh.Dog, "Berlin", Color.Yellow))
            .ToList();
        
        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        var dogPlaceColorProjector = new DogPlaceColorProjector(dogComparer);
        var dogProjector = new DogProjector(dogComparer);
        
        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            dogPlaceColorProjector);
        
        Assert.That(hashTable, Is.EqualTo(DogPlaceColorTuples.ExpectedHashTableSource));

        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
            
            var (dog, dogHc) = Dogs.NewDogsWithHashCode[0];
            Assert.That(dogHc, Is.EqualTo(4));
            
            var candidateDataIndex = TableHandling<DogPlaceColorTuple, (uint, uint, uint), ValueTuple<int>>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                dogPlaceColorProjector.ComputeHashTuple((dog, "Montevideo", Color.Thistle)),
                ref dataCount);

            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(TableHandling.CreateForEmptyEntry(dogHc, 1)));

            TableHandling<DogPlaceColorEntry, Dog>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                result,
                candidateDataIndex);

            Assert.That(hashTableCopy[4],
                Is.EqualTo(new HashEntry { DriftPlusOne = 1, ForwardIndex = candidateDataIndex }));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            DogPlaceColorGeneration.CheckTablesConsistencyForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            var (dog, dogHc) = Dogs.NewDogsWithHashCode[1];
            Assert.That(dogHc, Is.EqualTo(3));
            
            var candidateDataIndex = TableHandling<DogPlaceColorTuple, (uint, uint, uint), ValueTuple<int>>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                dogPlaceColorProjector.ComputeHashTuple((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
            
            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(TableHandling.CreateForEmptyEntry(dogHc + 1, 2)));

            TableHandling<DogPlaceColorEntry, Dog>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                result,
                candidateDataIndex);

            Assert.That(hashTableCopy[4],
                Is.EqualTo(new HashEntry { DriftPlusOne = 2, ForwardIndex = candidateDataIndex }));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            DogPlaceColorGeneration.CheckTablesConsistencyForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
        
            var (dog, dogHc) = Dogs.NewDogsWithHashCode[2];
            Assert.That(dogHc, Is.EqualTo(1));
        
            var candidateDataIndex = TableHandling<DogPlaceColorTuple, (uint, uint, uint), ValueTuple<int>>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                dogPlaceColorProjector.ComputeHashTuple((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
        
            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(TableHandling.CreateWhenSearchStopped(dogHc + 2, 3)));
        
            TableHandling<DogPlaceColorEntry, Dog>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                result,
                candidateDataIndex);
        
            Assert.That(hashTableCopy[3],
                Is.EqualTo(new HashEntry { DriftPlusOne = 3, ForwardIndex = candidateDataIndex }));
            Assert.That(hashTableCopy[4],
                Is.EqualTo(new HashEntry { DriftPlusOne = 3, ForwardIndex = 2 }));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 3, 4);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
        
            var (dog, dogHc) = Dogs.NewDogsWithHashCode[3];
            Assert.That(dogHc, Is.EqualTo(5));
        
            var candidateDataIndex = TableHandling<DogPlaceColorTuple, (uint, uint, uint), ValueTuple<int>>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                dogPlaceColorProjector.ComputeHashTuple((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
        
            var result = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(TableHandling.CreateWhenSearchStopped(dogHc + 3, 4)));
        
            TableHandling<DogPlaceColorEntry, Dog>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                result,
                candidateDataIndex);
        
            Assert.That(hashTableCopy[8],
                Is.EqualTo(new HashEntry { DriftPlusOne = 4, ForwardIndex = candidateDataIndex }));
            Assert.That(hashTableCopy[9],
                Is.EqualTo(new HashEntry { DriftPlusOne = 4, ForwardIndex = 6 }));
            Assert.That(hashTableCopy[10],
                Is.EqualTo(new HashEntry { DriftPlusOne = 3, ForwardIndex = 7 }));
            Assert.That(hashTableCopy[11],
                Is.EqualTo(new HashEntry { DriftPlusOne = 2, ForwardIndex = 8 }));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 8, 9, 10, 11);
        }
    }

    private void AreEqualExcept(
        IReadOnlyList<HashEntry> hashTableCopy,
        IReadOnlyList<HashEntry> hashTable,
        params int[] excluded)
    {
        HashSet<int> excludedIndexes = [..excluded];
        Assert.That(hashTableCopy.Count, Is.EqualTo(hashTable.Count));
        for (int i = 0; i < hashTable.Count; i++)
        {
            if (excludedIndexes.Contains(i))
                continue;
            Assert.That(hashTableCopy[i], Is.EqualTo(hashTable[i]));
        }
    }
}