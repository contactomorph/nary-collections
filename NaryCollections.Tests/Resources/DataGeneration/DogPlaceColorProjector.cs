using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

internal sealed class DogPlaceColorProjector
    : ICompleteDataProjector<DogPlaceColorTuple, (uint, uint, uint), ValueTuple<int>>
{
    private readonly IEqualityComparer<Dog> _dogComparer = EqualityComparer<Dog>.Default;
    private readonly IEqualityComparer<string> _stringComparer = EqualityComparer<string>.Default;
    private readonly IEqualityComparer<Color> _colorComparer = EqualityComparer<Color>.Default;

    public (DogPlaceColorTuple Item, uint HashCode) GetDataAt(DogPlaceColorEntry[] dataTable, int index)
    {
        return (dataTable[index].DataTuple, (uint)dataTable[index].HashTuple.GetHashCode());
    }

    public bool AreDataEqualAt(DogPlaceColorEntry[] dataTable, int index, DogPlaceColorTuple item, uint hashCode)
    {
        return (uint)dataTable[index].HashTuple.GetHashCode() == hashCode
               && _dogComparer.Equals(dataTable[index].DataTuple.Dog, item.Dog) &&
               _stringComparer.Equals(dataTable[index].DataTuple.Place, item.Place) &&
               _colorComparer.Equals(dataTable[index].DataTuple.Color, item.Color);
    }

    public int GetBackIndex(DogPlaceColorEntry[] dataTable, int index) => dataTable[index].BackIndexesTuple.Item1;

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, int backIndex)
    {
        dataTable[index].BackIndexesTuple.Item1 = backIndex;
    }

    public uint ComputeHashCode(DogPlaceColorTuple item) => (uint)ComputeHashTuple(item).GetHashCode();

    public void SetDataAt(
        DogPlaceColorEntry[] dataTable,
        int index, DogPlaceColorTuple dataTuple,
        (uint, uint, uint) hashTuple)
    {
        dataTable[index].DataTuple = dataTuple;
        dataTable[index].HashTuple = hashTuple;
    }

    public (uint, uint, uint) ComputeHashTuple(DogPlaceColorTuple dataTuple)
    {
        return (
            (uint)_dogComparer.GetHashCode(dataTuple.Dog),
            (uint)_stringComparer.GetHashCode(dataTuple.Place),
            (uint)_colorComparer.GetHashCode(dataTuple.Color)
        );
    }
}