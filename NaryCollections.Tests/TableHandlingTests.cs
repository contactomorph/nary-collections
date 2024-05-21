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
    [SetUp]
    public void Setup()
    {
    }
    
    [Test]
    public void CheckItemExistenceForUniqueParticipantTest()
    {
        DogPlaceColorTuple[] dataWithUniqueDogs = DogPlaceColorTuples.Data.DistinctBy(t => t.Dog).ToArray();

        DogPlaceColorGeneration.CreateTablesForUnique(
            dataWithUniqueDogs,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1);
        
        var projector = new DogProjector();

        foreach (var (dog, _, _) in dataWithUniqueDogs)
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

        DogPlaceColorGeneration.CheckTablesConsistencyForUnique(hashTable, dataTable, dataWithUniqueDogs.Length);
    }
    
    [Test]
    public void CheckItemExistenceForLineTest()
    {
        DogPlaceColorGeneration.CreateTablesForUnique(
            DogPlaceColorTuples.Data,
            out var hashTable,
            out var dataTable,
            hashTuple => (uint)hashTuple.GetHashCode());
        
        var projector = new DogPlaceColorProjector();

        foreach (var tuple in DogPlaceColorTuples.Data)
        {
            bool exists = TableHandling<DogPlaceColorEntry, DogPlaceColorTuple>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)DogPlaceColorGeneration.ToHashCodes(tuple).GetHashCode(),
                tuple);
        
            Assert.IsTrue(exists);
        }

        DogPlaceColorGeneration.CheckTablesConsistencyForUnique(hashTable, dataTable, DogPlaceColorTuples.Data.Length);
    }
}