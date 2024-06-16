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
        
        var projector = new DogProjector();

        foreach (var (dog, _, _) in DogPlaceColorTuples.DataWithUniqueDogs)
        {
            bool exists = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.IsTrue(exists);
        }
        
        foreach (var dog in Dogs.UnknownDogs)
        {
            bool exists = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.IsFalse(exists);
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
            bool exists = TableHandling<DogPlaceColorEntry, DogPlaceColorTuple>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)DogPlaceColorProjector.Instance.ComputeHashTuple(tuple).GetHashCode(),
                tuple);
        
            Assert.IsTrue(exists);
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
        
        var projector = new DogProjector();

        foreach (var dog in Dogs.KnownDogs)
        {
            bool exists = TableHandling<DogPlaceColorEntry, Dog>.ContainsForNonUnique(
                hashTable,
                correspondenceTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.IsTrue(exists, dog.ToString());
        }
        
        foreach (var dog in Dogs.UnknownDogs)
        {
            bool exists = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.IsFalse(exists);
        }

        DogPlaceColorGeneration.CheckTablesConsistencyForNonUnique(
            hashTable,
            correspondenceTable,
            dataTable,
            Dogs.KnownDogs.Count);
    }
}