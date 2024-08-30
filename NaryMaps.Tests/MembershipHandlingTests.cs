using System.Drawing;
using NaryMaps.Components;
using NaryMaps.Primitives;
using NaryMaps.Tests.Resources.Data;
using NaryMaps.Tests.Resources.DataGeneration;
using NaryMaps.Tests.Resources.Tools;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

public class MembershipHandlingTests
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
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.Find(
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
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.Find(
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
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, DogPlaceColorTuple, DogPlaceColorProjector>.Find(
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
        var data = Colors.KnownColors
            .SelectMany(color => new [] { (Dogs.KnownDogs[0], "Berlin", color), (Dogs.KnownDogs[1], "Berlin", color) })
            .ToList();
        
        DogPlaceColorGeneration.CreateTablesForNonUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item3,
            dataTuple => dataTuple.Color);
        
        var handler = ColorProjector.Instance;
        
        foreach (var color in Colors.KnownColors)
        {
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Color, ColorProjector>.Find(
                hashTable,
                dataTable,
                handler,
                (EqualityComparer<Dog>.Default, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                (uint)color.GetHashCode(),
                color);
        
            Assert.That(result.Case, Is.EqualTo(SearchCase.ItemFound));
        }
        
        foreach (var color in Colors.UnknownColors)
        {
            var result = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Color, ColorProjector>.Find(
                hashTable,
                dataTable,
                handler,
                (EqualityComparer<Dog>.Default, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                (uint)color.GetHashCode(),
                color);
        
            Assert.That(result.Case, Is.Not.EqualTo(SearchCase.ItemFound));
        }

        Consistency.CheckForNonUnique(
            hashTable,
            dataTable,
            data.Count,
            ColorProjector.Instance,
            DogPlaceColorProjector.GetHashTupleComputer());
    }
}