using System.Drawing;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Tools;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using HashTuple = (uint, uint, uint);
using IndexTuple = (int, MultiIndex);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

public class MultiUpdateHandlingTests
{
    private static List<DogPlaceColorTuple> DuplicateSomeDogs(IEnumerable<Dog> dogs)
    {
        static IEnumerable<DogPlaceColorTuple> Multiply(Dog dog, int i)
        {
            yield return (dog, "Paris", Color.Red);
            if (i % 2 == 0)
                yield return (dog, "Paris", Color.Blue);
            if (i % 3 == 0)
                yield return (dog, "Paris", Color.Green);
        }
        
        return dogs
            .SelectMany(Multiply)
            .ToList();
    }
    
    [Test]
    public void CheckItemAdditionForNonUniqueParticipantTest()
    {
        var data = DuplicateSomeDogs(Dogs.KnownDogsWithHashCode.Select(p => p.Dog));
        
        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        var comparerTuple = (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default);
        var hashTupleComputer = DogPlaceColorProjector.GetHashTupleComputer(dogComparer);
        
        DogPlaceColorGeneration.CreateTablesForNonUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            hashTupleComputer);

        Assert.That(hashTable, Is.EqualTo(DogPlaceColorTuples.ExpectedHashTableSource2));

        Consistency.CheckForNonUnique(
            hashTable,
            dataTable,
            data.Count,
            DogProjector.Instance,
            hashTupleComputer);

        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
            
            var (dog, dogHc) = Dogs.NewDogsWithHashCode[0];
            Assert.That(dogHc, Is.EqualTo(4));
            
            var candidateDataIndex = DataHandling<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                hashTupleComputer((dog, "Montevideo", Color.Thistle)),
                ref dataCount);

            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.Find(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                comparerTuple,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateForEmptyEntry(dogHc, 1)));

            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex,
                MultiIndex.NoNext);

            Consistency.EqualsAt(4, hashTableCopy, 1, candidateDataIndex);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 4);

            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
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
                hashTupleComputer((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
            
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.Find(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                comparerTuple,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateForEmptyEntry(dogHc + 1, 2)));

            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex,
                MultiIndex.NoNext);

            Consistency.EqualsAt(4, hashTableCopy, 2, candidateDataIndex);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 4);

            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
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
                hashTupleComputer((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
        
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.Find(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                comparerTuple,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateWhenSearchStopped(dogHc + 2, 3)));
        
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex,
                MultiIndex.NoNext);
        
            Consistency.EqualsAt(3, hashTableCopy, 3, candidateDataIndex);
            Consistency.EqualsAt(4, hashTableCopy, 3, 5);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 3, 4);

            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
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
                hashTupleComputer((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
        
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.Find(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                comparerTuple,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateWhenSearchStopped(dogHc + 3, 4)));
        
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex,
                MultiIndex.NoNext);
        
            Consistency.EqualsAt(8, hashTableCopy, 4, candidateDataIndex);
            Consistency.EqualsAt(9, hashTableCopy, 4, 13);
            Consistency.EqualsAt(10, hashTableCopy, 3, 14);
            Consistency.EqualsAt(11, hashTableCopy, 2, 16);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 8, 9, 10, 11);
            
            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
        }
        {
            // known dog
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();

            var (dog, dogHc) = Dogs.KnownDogsWithHashCode[4];
            Assert.That(dogHc, Is.EqualTo(5));
        
            var candidateDataIndex = DataHandling<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>.AddOnlyData(
                ref dataTableCopy,
                (dog, "Montevideo", Color.Thistle),
                hashTupleComputer((dog, "Montevideo", Color.Thistle)),
                ref dataCount);
        
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.Find(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                comparerTuple,
                dogHc,
                dog);
            
            Assert.That(result, Is.EqualTo(SearchResult.CreateForItemFound(dogHc + 1, 2, 9)));
            
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex,
                MultiIndex.NoNext);
            
            Consistency.EqualsAt(6, hashTableCopy, 2, candidateDataIndex);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 6);
            
            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
        }
    }
    
    [Test]
    public void CheckItemRemovalForNonUniqueParticipantTest()
    {
        var data = DuplicateSomeDogs(Dogs.KnownDogsWithHashCode.Select(p => p.Dog));
    
        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        var hashTupleComputer = DogPlaceColorProjector.GetHashTupleComputer(dogComparer);
        
        int hashEntryCount = DogPlaceColorGeneration.CreateTablesForNonUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            hashTupleComputer);
    
        Assert.That(hashTable, Is.EqualTo(DogPlaceColorTuples.ExpectedHashTableSource2));
        Assert.That(hashEntryCount, Is.EqualTo(9));
    
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
    
            int removedDataIndex = 1;
            
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                ref hashEntryCount,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
    
            DataHandling<DogPlaceColorTuple, HashTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.Optimal, removedDataIndex);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 10);
            
            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
        
            int removedDataIndex = 16;
            
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                ref hashEntryCount,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
            
            DataHandling<DogPlaceColorTuple, HashTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            removedDataIndex = 15;
            
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                ref hashEntryCount,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
            
            DataHandling<DogPlaceColorTuple, HashTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.DriftForUnused, 0);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 10);
            
            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
        
            int removedDataIndex = 7;
            
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                ref hashEntryCount,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
            
            DataHandling<DogPlaceColorTuple, HashTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            removedDataIndex = 6;
            
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                ref hashEntryCount,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
            
            DataHandling<DogPlaceColorTuple, HashTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(5, hashTableCopy, HashEntry.Optimal, 9);
            Consistency.EqualsAt(6, hashTableCopy, 2, 10);
            Consistency.EqualsAt(7, hashTableCopy, 2, 13);
            Consistency.EqualsAt(8, hashTableCopy, HashEntry.Optimal, 14);
            Consistency.EqualsAt(9, hashTableCopy, HashEntry.DriftForUnused, 0);
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.Optimal, 7);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 5, 6, 7, 8, 9, 10);
            
            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
        
            int removedDataIndex = 10;
            
            MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                ref hashEntryCount,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
            
            DataHandling<DogPlaceColorTuple, HashTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(7, hashTableCopy, 2, 13);
            Consistency.EqualsAt(8, hashTableCopy, HashEntry.Optimal, 14);
            Consistency.EqualsAt(9, hashTableCopy, HashEntry.DriftForUnused, 0);
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.Optimal, 10);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource2, 7, 8, 9, 10);
            
            Consistency.CheckForNonUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogProjector.Instance,
                hashTupleComputer);
        }
    }
    
    [Test]
    public void CapacityChangeForNonUniqueTest()
    {
        var hashTupleComputer = DogPlaceColorProjector.GetHashTupleComputer();
        
        DogPlaceColorGeneration.CreateTablesForNonUnique(
            DogPlaceColorTuples.Data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog);
        
        Consistency.CheckForNonUnique(
            hashTable,
            dataTable,
            DogPlaceColorTuples.Data.Count,
            DogProjector.Instance,
            hashTupleComputer);

        for (int i = 0; i < 5; ++i)
        {
            hashTable = MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.ChangeCapacity(
                dataTable,
                DogProjector.Instance,
                HashEntry.IncreaseCapacity(hashTable.Length),
                DogPlaceColorTuples.Data.Count);
        
            Consistency.CheckForNonUnique(
                hashTable,
                dataTable,
                DogPlaceColorTuples.Data.Count,
                DogProjector.Instance,
                hashTupleComputer);
        }

        for (int i = 0; i < 6; ++i)
        {
            hashTable = MultiUpdateHandling<DogPlaceColorEntry, DogProjector>.ChangeCapacity(
                dataTable,
                DogProjector.Instance,
                HashEntry.DecreaseCapacity(hashTable.Length),
                DogPlaceColorTuples.Data.Count);
        
            Consistency.CheckForNonUnique(
                hashTable,
                dataTable,
                DogPlaceColorTuples.Data.Count,
                DogProjector.Instance,
                hashTupleComputer);
        }
    }
}