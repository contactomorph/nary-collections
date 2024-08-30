using System.Drawing;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

internal readonly struct DogPlaceColorProjector :
    IDataEquator<DogPlaceColorEntry, ComparerTuple, DogPlaceColorTuple>,
    IResizeHandler<DogPlaceColorEntry, int>
{
    public static readonly DogPlaceColorProjector Instance = new();
    
    public uint GetHashCodeAt(DogPlaceColorEntry[] dataTable, int index)
    {
        return (uint)dataTable[index].HashTuple.GetHashCode();
    }

    public bool AreDataEqualAt(
        DogPlaceColorEntry[] dataTable,
        ComparerTuple comparerTuple,
        int index,
        DogPlaceColorTuple item,
        uint hashCode)
    {
        return (uint)dataTable[index].HashTuple.GetHashCode() == hashCode
               && comparerTuple.Item1.Equals(dataTable[index].DataTuple.Dog, item.Dog)
               && comparerTuple.Item2.Equals(dataTable[index].DataTuple.Place, item.Place)
               && comparerTuple.Item3.Equals(dataTable[index].DataTuple.Color, item.Color);
    }

    public int GetBackIndex(DogPlaceColorEntry[] dataTable, int index) => dataTable[index].BackIndexesTuple.Item1;

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, int backIndex)
    {
        dataTable[index].BackIndexesTuple.Item1 = backIndex;
    }

    public uint ComputeHashCode(ComparerTuple comparerTuple, DogPlaceColorTuple item)
    {
        return (uint)ComputeHashTuple(comparerTuple, item).GetHashCode();
    }

    private (uint, uint, uint) ComputeHashTuple(ComparerTuple comparerTuple, DogPlaceColorTuple dataTuple)
    {
        return (
            (uint)comparerTuple.Item1.GetHashCode(dataTuple.Dog),
            (uint)comparerTuple.Item2.GetHashCode(dataTuple.Place),
            (uint)comparerTuple.Item3.GetHashCode(dataTuple.Color)
        );
    }
    
    public static Func<DogPlaceColorTuple, (uint, uint, uint)> GetHashTupleComputer(
        IEqualityComparer<Dog>? dogComparer = null)
    {
        dogComparer ??= EqualityComparer<Dog>.Default;
        var comparerTuple = (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default);
        return dataTuple => (
            (uint)comparerTuple.Item1.GetHashCode(dataTuple.Dog),
            (uint)comparerTuple.Item2.GetHashCode(dataTuple.Place),
            (uint)comparerTuple.Item3.GetHashCode(dataTuple.Color)
        );
    }
}