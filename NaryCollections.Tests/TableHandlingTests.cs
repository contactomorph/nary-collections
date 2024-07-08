using System.Drawing;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Tools;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using HashColorTuple = (uint, uint, uint);
using IndexTuple = (int, MultiIndex);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

public class UpdateHandlingTests
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
        
        var handler = DogProjector.Instance;

        foreach (var (dog, _, _) in DogPlaceColorTuples.DataWithUniqueDogs)
        {
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTable,
                dataTable,
                handler,
                (EqualityComparer<Dog>.Default, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.EqualTo(SearchCase.ItemFound));
        }
        
        foreach (var dog in Dogs.UnknownDogs)
        {
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTable,
                dataTable,
                handler,
                (EqualityComparer<Dog>.Default, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.Not.EqualTo(SearchCase.ItemFound));
        }

        Consistency.CheckForUnique(
            hashTable,
            dataTable,
            DogPlaceColorTuples.DataWithUniqueDogs.Count,
            DogPlaceColorProjector.Instance,
            DogPlaceColorProjector.GetHashTupleComputer());
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
        
        var handler = DogPlaceColorProjector.Instance;

        foreach (var tuple in DogPlaceColorTuples.Data)
        {
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, DogPlaceColorTuple, DogPlaceColorProjector>.ContainsForUnique(
                hashTable,
                dataTable,
                handler,
                (EqualityComparer<Dog>.Default, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                (uint)DogPlaceColorProjector.GetHashTupleComputer()(tuple).GetHashCode(),
                tuple);
        
            Assert.That(result.Case, Is.EqualTo(SearchCase.ItemFound));
        }

        Consistency.CheckForUnique(
            hashTable,
            dataTable,
            DogPlaceColorTuples.Data.Count,
            DogPlaceColorProjector.Instance,
            DogPlaceColorProjector.GetHashTupleComputer());
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
        
        var handler = DogProjector.Instance;

        foreach (var dog in Dogs.KnownDogs)
        {
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForNonUnique(
                hashTable,
                correspondenceTable,
                dataTable,
                handler,
                (EqualityComparer<Dog>.Default, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.EqualTo(SearchCase.ItemFound));
        }
        
        foreach (var dog in Dogs.UnknownDogs)
        {
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTable,
                dataTable,
                handler,
                (EqualityComparer<Dog>.Default, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.That(result.Case, Is.Not.EqualTo(SearchCase.ItemFound));
        }

        Consistency.CheckForNonUnique(
            hashTable,
            correspondenceTable,
            dataTable,
            Dogs.KnownDogs.Count,
            DogPlaceColorProjector.Instance,
            DogPlaceColorProjector.GetHashTupleComputer());
    }

    [Test]
    public void CheckItemAdditionForUniqueParticipantTest()
    {
        var data = Dogs.KnownDogsWithHashCode
            .Select(dh => (dh.Dog, "Berlin", Color.Yellow))
            .ToList();
        
        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        
        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        
        Assert.That(hashTable, Is.EqualTo(DogPlaceColorTuples.ExpectedHashTableSource));

        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
            
            var (dog, dogHc) = Dogs.NewDogsWithHashCode[0];
            Assert.That(dogHc, Is.EqualTo(4));
            
            var candidateDataIndex = DataHandling<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer)((dog, "Montevideo", Color.Thistle)),
                ref dataCount);

            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateForEmptyEntry(dogHc, 1)));

            UpdateHandling<DogPlaceColorEntry, DogProjector>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex);

            EqualsAt(4, hashTableCopy, (1, candidateDataIndex));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            var (dog, dogHc) = Dogs.NewDogsWithHashCode[1];
            Assert.That(dogHc, Is.EqualTo(3));
            
            var candidateDataIndex = DataHandling<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer)((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
            
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateForEmptyEntry(dogHc + 1, 2)));

            UpdateHandling<DogPlaceColorEntry, DogProjector>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex);

            EqualsAt(4, hashTableCopy, (2, candidateDataIndex));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
        
            var (dog, dogHc) = Dogs.NewDogsWithHashCode[2];
            Assert.That(dogHc, Is.EqualTo(1));
        
            var candidateDataIndex = DataHandling<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer)((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
        
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateWhenSearchStopped(dogHc + 2, 3)));
        
            UpdateHandling<DogPlaceColorEntry, DogProjector>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex);
        
            EqualsAt(3, hashTableCopy, (3, candidateDataIndex));
            EqualsAt(4, hashTableCopy, (3, 2));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 3, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
        
            var (dog, dogHc) = Dogs.NewDogsWithHashCode[3];
            Assert.That(dogHc, Is.EqualTo(5));
        
            var candidateDataIndex = DataHandling<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer)((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
        
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateWhenSearchStopped(dogHc + 3, 4)));
        
            UpdateHandling<DogPlaceColorEntry, DogProjector>.AddForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
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
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
    }
    
    [Test]
    public void CheckItemRemovalForUniqueParticipantTest()
    {
        var data = Dogs.KnownDogsWithHashCode
            .Select(dh => (dh.Dog, "Berlin", Color.Yellow))
            .ToList();

        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        
        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            DogPlaceColorProjector.GetHashTupleComputer(dogComparer));

        Assert.That(hashTable, Is.EqualTo(DogPlaceColorTuples.ExpectedHashTableSource));

        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            int dataIndexToRemove = 2;
            var successfulResult = SearchResult.CreateForItemFound(
                (uint)dataTableCopy[dataIndexToRemove].BackIndexesTuple.Item1,
                0,
                dataIndexToRemove);

            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            UpdateHandling<DogPlaceColorEntry, DogProjector>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                successfulResult, 
                dataCount);
            
            EqualsAt(3, hashTableCopy, (HashEntry.DriftForUnused, 0));
            EqualsAt(10, hashTableCopy, (HashEntry.Optimal, 2));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 3, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            int dataIndexToRemove = 8;
            var successfulResult = SearchResult.CreateForItemFound(
                (uint)dataTableCopy[dataIndexToRemove].BackIndexesTuple.Item1,
                0,
                dataIndexToRemove);
            
            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            UpdateHandling<DogPlaceColorEntry, DogProjector>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                successfulResult,
                newDataCount: dataCount);
            
            EqualsAt(10, hashTableCopy, (HashEntry.DriftForUnused, 0));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            int dataIndexToRemove = 3;
            var successfulResult = SearchResult.CreateForItemFound(
                (uint)dataTableCopy[dataIndexToRemove].BackIndexesTuple.Item1,
                0,
                dataIndexToRemove);

            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            UpdateHandling<DogPlaceColorEntry, DogProjector>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                successfulResult,
                newDataCount: dataCount);
            
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
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            int dataIndexToRemove = 5;
            var successfulResult = SearchResult.CreateForItemFound(
                (uint)dataTableCopy[dataIndexToRemove].BackIndexesTuple.Item1,
                0,
                dataIndexToRemove);

            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                dataIndexToRemove,
                ref dataCount);
            
            UpdateHandling<DogPlaceColorEntry, DogProjector>.RemoveForUnique(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                successfulResult,
                newDataCount: dataCount);
            
            EqualsAt(7, hashTableCopy, (2, 6));
            EqualsAt(8, hashTableCopy, (HashEntry.Optimal, 7));
            EqualsAt(9, hashTableCopy, (HashEntry.DriftForUnused, 0));
            EqualsAt(10, hashTableCopy, (HashEntry.Optimal, 5));
            AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 7, 8, 9, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer(dogComparer));
        }
    }

    [Test]
    public void CapacityChangeForUniqueTest()
    {
        DogPlaceColorGeneration.CreateTablesForUnique(
            DogPlaceColorTuples.Data,
            out var hashTable,
            out var dataTable,
            hashTuple => (uint)hashTuple.GetHashCode(),
            dataTuple => dataTuple);

        for (int i = 0; i < 5; ++i)
        {
            hashTable = UpdateHandling<DogPlaceColorEntry, DogPlaceColorProjector>.ChangeCapacityForUnique(
                dataTable,
                DogPlaceColorProjector.Instance,
                HashEntry.IncreaseCapacity(hashTable.Length),
                DogPlaceColorTuples.Data.Count);
        
            Consistency.CheckForUnique(
                hashTable,
                dataTable,
                DogPlaceColorTuples.Data.Count,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer());
        }

        for (int i = 0; i < 5; ++i)
        {
            hashTable = UpdateHandling<DogPlaceColorEntry, DogPlaceColorProjector>.ChangeCapacityForUnique(
                dataTable,
                DogPlaceColorProjector.Instance,
                HashEntry.DecreaseCapacity(hashTable.Length),
                DogPlaceColorTuples.Data.Count);
        
            Consistency.CheckForUnique(
                hashTable,
                dataTable,
                DogPlaceColorTuples.Data.Count,
                DogPlaceColorProjector.Instance,
                DogPlaceColorProjector.GetHashTupleComputer());
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