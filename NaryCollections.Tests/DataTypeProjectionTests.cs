using System.Drawing;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Types;
using NaryCollections.Tools;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using IndexTuple = (int, MultiIndex, MultiIndex, int);
using ColorDogTuple = (Color Color, Dog Dog);

public class DataTypeProjectionTests
{
    [Test]
    public void CreateProjectionsForDogPlaceColorTupleTest()
    {
        var p1 = new DataTypeProjection(
            typeof(DogPlaceColorTuple),
            3,
            [false, true, true, false],
            [2, 0]);
        
        Assert.That(p1.DataTupleType, Is.EqualTo(ValueTupleType.From(typeof(DogPlaceColorTuple))));
        Assert.That(
            p1.ComparerTypes,
            Is.EquivalentTo(new[]
            {
                typeof(IEqualityComparer<Dog>),
                typeof(IEqualityComparer<string>),
                typeof(IEqualityComparer<Color>),
            }));
        Assert.That(p1.HashTupleType, Is.EqualTo(ValueTupleType.From(typeof((uint, uint, uint)))));
        Assert.That(p1.BackIndexTupleType, Is.EqualTo(ValueTupleType.From(typeof(IndexTuple))));
        
        Assert.That(
            p1.DataEntryType,
            Is.EqualTo(typeof(DataEntry<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>)));
        Assert.That(
            p1.DataTableType,
            Is.EqualTo(typeof(DataEntry<DogPlaceColorTuple, (uint, uint, uint), IndexTuple>[])));
        
        Assert.That(
            p1.DataProjectionMapping,
            Is.EquivalentTo(new[]
            {
                (typeof(Color), 0, typeof(ColorDogTuple).GetField("Item1"), 2, typeof(DogPlaceColorTuple).GetField("Item3")),
                (typeof(Dog), 1, typeof(ColorDogTuple).GetField("Item2"), 0, typeof(DogPlaceColorTuple).GetField("Item1")),
            }));
        Assert.That(
            p1.HashProjectionMapping,
            Is.EquivalentTo(new[]
            {
                (typeof(uint), 0, typeof((uint, uint)).GetField("Item1"), 2, typeof((uint, uint, uint)).GetField("Item3")),
                (typeof(uint), 1, typeof((uint, uint)).GetField("Item2"), 0, typeof((uint, uint, uint)).GetField("Item1")),
            }));
        Assert.That(p1.BackIndexProjectionField, Is.EqualTo(typeof(IndexTuple).GetField("Item4")));
    }
}