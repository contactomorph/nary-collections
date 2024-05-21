using System.Drawing;
using NaryCollections.Details;
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
        Dog[] knownDogs =
        [
            new("Rex", "Anne Besnard"),
            new("Fifi", "Louis Durand"),
            new("Platon", "Jean Dupuis"),
        ];

        Dog[] unknownDogs =
        [
            new("Fifi", "Louis Dupont"),
            new("Rex", "Jean Dupuis"),
        ];

        DogPlaceColorTuple[] data =
        [
            (knownDogs[0], "Lyon", Color.Beige),
            (knownDogs[1], "Lyon", Color.CadetBlue),
            (knownDogs[2], "Paris", Color.Beige),
        ];

        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1);
        
        var projector = new DogProjector();

        foreach (var (dog, _, _) in data)
        {
            bool exists = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.IsTrue(exists);
        }
        
        foreach (var dog in unknownDogs)
        {
            bool exists = TableHandling<DogPlaceColorEntry, Dog>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)dog.GetHashCode(),
                dog);
        
            Assert.IsFalse(exists);
        }

        DogPlaceColorGeneration.CheckTablesConsistencyForUnique(hashTable, dataTable, data.Length);
    }
    
    [Test]
    public void CheckItemExistenceForLineTest()
    {
        Dog[] knownDogs =
        [
            new("Rex", "Anne Besnard"),
            new("Fifi", "Louis Durand"),
            new("Platon", "Jean Dupuis"),
        ];

        DogPlaceColorTuple[] data =
        [
            (knownDogs[0], "Lyon", Color.Beige),
            (knownDogs[1], "Lyon", Color.CadetBlue),
            (knownDogs[2], "Paris", Color.Beige),
            (knownDogs[2], "Lyon", Color.CadetBlue),
            (knownDogs[1], "Lyon", Color.Orange),
            (knownDogs[1], "Bordeaux", Color.CadetBlue),
            (knownDogs[0], "Bordeaux", Color.Beige),
            (knownDogs[0], "Bordeaux", Color.CadetBlue),
        ];

        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => (uint)hashTuple.GetHashCode());
        
        var projector = new DogPlaceColorProjector();

        foreach (var tuple in data)
        {
            bool exists = TableHandling<DogPlaceColorEntry, DogPlaceColorTuple>.ContainsForUnique(
                hashTable,
                dataTable,
                projector,
                (uint)DogPlaceColorGeneration.ToHashCodes(tuple).GetHashCode(),
                tuple);
        
            Assert.IsTrue(exists);
        }

        DogPlaceColorGeneration.CheckTablesConsistencyForUnique(hashTable, dataTable, data.Length);
    }
}