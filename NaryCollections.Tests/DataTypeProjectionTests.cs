using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;
using NaryCollections.Tools;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);

public class DataTypeProjectionTests
{
    [Test]
    public void CreateProjectionsForDogPlaceColorTupleTest()
    {
        var p1 = new DataTypeProjection(typeof(DogPlaceColorTuple), 3, 4, [2, 0]);
        
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
        Assert.That(p1.BackIndexTupleType, Is.EqualTo(ValueTupleType.From(typeof((int, int, int, int)))));
        
        Assert.That(
            p1.DataEntryType,
            Is.EqualTo(typeof(DataEntry<DogPlaceColorTuple, (uint, uint, uint), (int, int, int, int)>)));
        Assert.That(
            p1.DataTableType,
            Is.EqualTo(typeof(DataEntry<DogPlaceColorTuple, (uint, uint, uint), (int, int, int, int)>[])));
        
        Assert.That(
            p1.DataProjectionMapping.MappingFields,
            Is.EquivalentTo(new[]
            {
                typeof(DogPlaceColorTuple).GetField("Item3"),
                typeof(DogPlaceColorTuple).GetField("Item1"),
            }));
        Assert.That(
            p1.HashProjectionMapping.MappingFields,
            Is.EquivalentTo(new[]
            {
                typeof((uint, uint, uint)).GetField("Item3"),
                typeof((uint, uint, uint)).GetField("Item1"),
            }));
        Assert.That(p1.BackIndexProjectionField, Is.EqualTo(typeof((int, int, int, int)).GetField("Item4")));
    }
}