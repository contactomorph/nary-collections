using System.Drawing;
using NaryMaps.Components;
using NaryMaps.Primitives;
using NaryMaps.Tests.Resources.Data;
using NaryMaps.Tests.Resources.DataGeneration;
using NaryMaps.Tests.Resources.Tools;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using HashColorTuple = (uint, uint, uint);
using IndexTuple = (int, MultiIndex);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

public class MonoUpdateHandlingTests
{
    [Test]
    public void CheckItemAdditionForUniqueParticipantTest()
    {
        var data = Dogs.KnownDogsWithHashCode
            .Select(dh => (dh.Dog, "Berlin", Color.Yellow))
            .ToList();
        
        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        var comparerTuple = (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default);
        var hashTupleComputer = DogPlaceColorProjector.GetHashTupleComputer(dogComparer);
        
        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            hashTupleComputer);
        
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

            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex);

            Consistency.EqualsAt(4, hashTableCopy, 1, candidateDataIndex);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
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

            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex);

            Consistency.EqualsAt(4, hashTableCopy, 2, candidateDataIndex);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
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
        
            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex);
        
            Consistency.EqualsAt(3, hashTableCopy, 3, candidateDataIndex);
            Consistency.EqualsAt(4, hashTableCopy, 3, 2);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 3, 4);

            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
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
        
            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.AddStrictly(
                hashTableCopy,
                dataTableCopy,
                DogProjector.Instance,
                result,
                candidateDataIndex);
        
            Consistency.EqualsAt(8, hashTableCopy, 4, candidateDataIndex);
            Consistency.EqualsAt(9, hashTableCopy, 4, 6);
            Consistency.EqualsAt(10, hashTableCopy, 3, 7);
            Consistency.EqualsAt(11, hashTableCopy, 2, 8);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 8, 9, 10, 11);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                hashTupleComputer);
        }
    }
    
    [Test]
    public void CheckItemRemovalForUniqueParticipantTest()
    {
        var data = Dogs.KnownDogsWithHashCode
            .Select(dh => (dh.Dog, "Berlin", Color.Yellow))
            .ToList();

        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        var hashTupleComputer = DogPlaceColorProjector.GetHashTupleComputer(dogComparer);
        
        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            hashTupleComputer);

        Assert.That(hashTable, Is.EqualTo(DogPlaceColorTuples.ExpectedHashTableSource));

        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
    
            int removedDataIndex = 2;
            
            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
    
            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(3, hashTableCopy, HashEntry.DriftForUnused, 0);
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.Optimal, 2);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 3, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                hashTupleComputer);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
    
            int removedDataIndex = 8;
            
            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
    
            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.DriftForUnused, 0);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                hashTupleComputer);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
    
            int removedDataIndex = 3;
            
            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
    
            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(5, hashTableCopy, HashEntry.Optimal, 4);
            Consistency.EqualsAt(6, hashTableCopy, 2, 5);
            Consistency.EqualsAt(7, hashTableCopy, 2, 6);
            Consistency.EqualsAt(8, hashTableCopy, HashEntry.Optimal, 7);
            Consistency.EqualsAt(9, hashTableCopy, HashEntry.DriftForUnused, 0);
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.Optimal, 3);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 5, 6, 7, 8, 9, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                hashTupleComputer);
        }
        {
            int dataCount = data.Count;
            var hashTableCopy = hashTable.ToArray();
            var dataTableCopy = dataTable.ToArray();
    
            int removedDataIndex = 5;
            
            MonoUpdateHandling<DogPlaceColorEntry, DogProjector>.Remove(
                ref hashTableCopy,
                dataCount,
                dataTableCopy,
                DogProjector.Instance,
                removedDataIndex);
    
            DataHandling<DogPlaceColorTuple, HashColorTuple, IndexTuple>.RemoveOnlyData(
                ref dataTableCopy,
                removedDataIndex,
                ref dataCount);
            
            Consistency.EqualsAt(7, hashTableCopy, 2, 6);
            Consistency.EqualsAt(8, hashTableCopy, HashEntry.Optimal, 7);
            Consistency.EqualsAt(9, hashTableCopy, HashEntry.DriftForUnused, 0);
            Consistency.EqualsAt(10, hashTableCopy, HashEntry.Optimal, 5);
            Consistency.AreEqualExcept(hashTableCopy, DogPlaceColorTuples.ExpectedHashTableSource, 7, 8, 9, 10);
            
            Consistency.CheckForUnique(
                hashTableCopy,
                dataTableCopy,
                dataCount,
                DogPlaceColorProjector.Instance,
                hashTupleComputer);
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
            hashTable = MonoUpdateHandling<DogPlaceColorEntry, DogPlaceColorProjector>.ChangeCapacity(
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
            hashTable = MonoUpdateHandling<DogPlaceColorEntry, DogPlaceColorProjector>.ChangeCapacity(
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
}