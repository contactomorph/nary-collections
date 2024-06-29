using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Tools;
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

        Consistency.CheckForUnique(
            hashTable,
            dataTable,
            DogPlaceColorTuples.DataWithUniqueDogs.Count,
            DogPlaceColorProjector.Instance,
            DogPlaceColorProjector.Instance.ComputeHashTuple);
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

        Consistency.CheckForUnique(
            hashTable,
            dataTable,
            DogPlaceColorTuples.Data.Count,
            DogPlaceColorProjector.Instance,
            DogPlaceColorProjector.Instance.ComputeHashTuple);
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

        Consistency.CheckForNonUnique(
            hashTable,
            correspondenceTable,
            dataTable,
            Dogs.KnownDogs.Count,
            DogPlaceColorProjector.Instance,
            DogPlaceColorProjector.Instance.ComputeHashTuple);
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

            EqualsAt(4, hashTableCopy, (1, candidateDataIndex));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
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

            EqualsAt(4, hashTableCopy, (2, candidateDataIndex));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
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
        
            EqualsAt(3, hashTableCopy, (3, candidateDataIndex));
            EqualsAt(4, hashTableCopy, (3, 2));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 3, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
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
        
            EqualsAt(8, hashTableCopy, (4, candidateDataIndex));
            EqualsAt(9, hashTableCopy, (4, 6));
            EqualsAt(10, hashTableCopy, (3, 7));
            EqualsAt(11, hashTableCopy, (2, 8));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 8, 9, 10, 11);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }
    }
    
    [Test]
    public void CheckItemRemovalForUniqueParticipantTest()
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

            int dataIndexToRemove = 2;
            
            TableHandling<DogPlaceColorEntry, Dog>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dataIndexToRemove,
                dataCount);

            TableHandling<DogPlaceColorEntry, Dog>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            EqualsAt(3, hashTableCopy, (HashEntry.DriftForUnused, 0));
            EqualsAt(10, hashTableCopy, (HashEntry.Optimal, 2));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 3, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            int dataIndexToRemove = 8;
            
            TableHandling<DogPlaceColorEntry, Dog>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dataIndexToRemove,
                dataCount);

            TableHandling<DogPlaceColorEntry, Dog>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            EqualsAt(10, hashTableCopy, (HashEntry.DriftForUnused, 0));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            int dataIndexToRemove = 3;
            
            TableHandling<DogPlaceColorEntry, Dog>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dataIndexToRemove,
                dataCount);

            TableHandling<DogPlaceColorEntry, Dog>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            EqualsAt(5, hashTableCopy, (HashEntry.Optimal, 4));
            EqualsAt(6, hashTableCopy, (2, 5));
            EqualsAt(7, hashTableCopy, (2, 6));
            EqualsAt(8, hashTableCopy, (HashEntry.Optimal, 7));
            EqualsAt(9, hashTableCopy, (HashEntry.DriftForUnused, 0));
            EqualsAt(10, hashTableCopy, (HashEntry.Optimal, 3));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 5, 6, 7, 8, 9, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            int dataIndexToRemove = 5;
            
            TableHandling<DogPlaceColorEntry, Dog>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                dogProjector,
                dataIndexToRemove,
                dataCount);

            TableHandling<DogPlaceColorEntry, Dog>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            EqualsAt(7, hashTableCopy, (2, 6));
            EqualsAt(8, hashTableCopy, (HashEntry.Optimal, 7));
            EqualsAt(9, hashTableCopy, (HashEntry.DriftForUnused, 0));
            EqualsAt(10, hashTableCopy, (HashEntry.Optimal, 5));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 7, 8, 9, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }
    }

    [Test]
    public void CapacityChangeForUniqueTest()
    {
        var dogPlaceColorProjector = new DogPlaceColorProjector();

        DogPlaceColorGeneration.CreateTablesForUnique(
            DogPlaceColorTuples.Data,
            out var hashTable,
            out var dataTable,
            hashTuple => (uint)hashTuple.GetHashCode(),
            dataTuple => dataTuple,
            dogPlaceColorProjector);

        for (int i = 0; i < 5; ++i)
        {
            TableHandling<DogPlaceColorEntry, DogPlaceColorTuple>.ChangeCapacityForUnique(
                ref hashTable,
                dataTable,
                dogPlaceColorProjector,
                HashEntry.IncreaseCapacity(hashTable.Length),
                DogPlaceColorTuples.Data.Count);
        
            Consistency.CheckForUnique(
                hashTable,
                dataTable,
                DogPlaceColorTuples.Data.Count,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }

        for (int i = 0; i < 5; ++i)
        {
            TableHandling<DogPlaceColorEntry, DogPlaceColorTuple>.ChangeCapacityForUnique(
                ref hashTable,
                dataTable,
                dogPlaceColorProjector,
                HashEntry.DecreaseCapacity(hashTable.Length),
                DogPlaceColorTuples.Data.Count);
        
            Consistency.CheckForUnique(
                hashTable,
                dataTable,
                DogPlaceColorTuples.Data.Count,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }
    }

    private void EqualsAt(
        int index,
        IReadOnlyList<HashEntry> hashTableCopy,
        (uint DriftPlusOne, int ForwardIndex) entry)
    {
        var expectedEntry = new HashEntry
        {
            DriftPlusOne = entry.DriftPlusOne,
            ForwardIndex = entry.ForwardIndex,
        };
        Assert.That(hashTableCopy[index], Is.EqualTo(expectedEntry));
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