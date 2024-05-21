using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);

public class DataTypeProjectionTests
{
    [Test]
    public void CreateProjectionsForDogPlaceColorTupleTest()
    {
        var p1 = new DataTypeProjection(typeof(DogPlaceColorTuple), 3, 4, [2, 0]);
        
        Assert.That(p1.DataTupleType, Is.EqualTo(typeof(DogPlaceColorTuple)));
        Assert.That(
            p1.DataTypes,
            Is.EquivalentTo(new[] { typeof(Dog), typeof(string), typeof(Color) }));
        Assert.That(
            p1.ComparerTypes,
            Is.EquivalentTo(new[]
            {
                typeof(IEqualityComparer<Dog>),
                typeof(IEqualityComparer<string>),
                typeof(IEqualityComparer<Color>),
            }));
        Assert.That(
            p1.ComparerMethods,
            Is.EquivalentTo(new[]
            {
                typeof(IEqualityComparer<Dog>).GetMethod("Equals", [typeof(Dog), typeof(Dog)]),
                typeof(IEqualityComparer<string>).GetMethod("Equals", [typeof(string), typeof(string)]),
                typeof(IEqualityComparer<Color>).GetMethod("Equals", [typeof(Color), typeof(Color)]),
            }));
        Assert.That(
            p1.ComparerMethods,
            Is.EquivalentTo(new[]
            {
                typeof(IEqualityComparer<Dog>).GetMethod("Equals", [typeof(Dog), typeof(Dog)]),
                typeof(IEqualityComparer<string>).GetMethod("Equals", [typeof(string), typeof(string)]),
                typeof(IEqualityComparer<Color>).GetMethod("Equals", [typeof(Color), typeof(Color)]),
            }));
        Assert.That(p1.HashTupleType, Is.EqualTo(typeof((uint, uint, uint))));
        Assert.That(p1.BackIndexTupleType, Is.EqualTo(typeof((int, int, int, int))));
        
        Assert.That(
            p1.DataEntryType,
            Is.EqualTo(typeof(DataEntry<DogPlaceColorTuple, (uint, uint, uint), (int, int, int, int)>)));
        Assert.That(
            p1.DataTableType,
            Is.EqualTo(typeof(DataEntry<DogPlaceColorTuple, (uint, uint, uint), (int, int, int, int)>[])));
        
        Assert.That(
            p1.DataProjectionFields,
            Is.EquivalentTo(new[]
            {
                typeof(DogPlaceColorTuple).GetField("Item3"),
                typeof(DogPlaceColorTuple).GetField("Item1"),
            }));
        Assert.That(
            p1.DataProjectionTypes,
            Is.EquivalentTo(new[] { typeof(Color), typeof(Dog) }));
        Assert.That(
            p1.HashProjectionFields,
            Is.EquivalentTo(new[]
            {
                typeof((uint, uint, uint)).GetField("Item3"),
                typeof((uint, uint, uint)).GetField("Item1"),
            }));
        Assert.That(p1.BackIndexProjectionField, Is.EqualTo(typeof((int, int, int, int)).GetField("Item4")));
    }
}