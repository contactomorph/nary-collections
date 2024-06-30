using System.Drawing;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

internal readonly struct DogPlaceColorProjector : IDataProjector<DogPlaceColorEntry, DogPlaceColorTuple>
{
    public static readonly DogPlaceColorProjector Instance = new(
        EqualityComparer<Dog>.Default,
        EqualityComparer<string>.Default,
        EqualityComparer<Color>.Default);
    
    private readonly IEqualityComparer<Dog> _dogComparer;
    private readonly IEqualityComparer<string> _stringComparer;
    private readonly IEqualityComparer<Color> _colorComparer;
    
    public DogPlaceColorProjector(
        IEqualityComparer<Dog>? dogComparer = null,
        IEqualityComparer<string>? stringComparer = null,
        IEqualityComparer<Color>? colorComparer = null)
    {
        _dogComparer = dogComparer ?? EqualityComparer<Dog>.Default;
        _stringComparer = stringComparer ?? EqualityComparer<string>.Default;
        _colorComparer = colorComparer ?? EqualityComparer<Color>.Default;
    }

    public uint GetHashCodeAt(DogPlaceColorEntry[] dataTable, int index)
    {
        return (uint)dataTable[index].HashTuple.GetHashCode();
    }

    public bool AreDataEqualAt(DogPlaceColorEntry[] dataTable, int index, DogPlaceColorTuple item, uint hashCode)
    {
        return (uint)dataTable[index].HashTuple.GetHashCode() == hashCode
               && _dogComparer.Equals(dataTable[index].DataTuple.Dog, item.Dog)
               && _stringComparer.Equals(dataTable[index].DataTuple.Place, item.Place)
               && _colorComparer.Equals(dataTable[index].DataTuple.Color, item.Color);
    }

    public int GetBackIndex(DogPlaceColorEntry[] dataTable, int index) => dataTable[index].BackIndexesTuple.Item1;

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, int backIndex)
    {
        dataTable[index].BackIndexesTuple.Item1 = backIndex;
    }

    public uint ComputeHashCode(DogPlaceColorTuple item) => (uint)ComputeHashTuple(item).GetHashCode();

    public (uint, uint, uint) ComputeHashTuple(DogPlaceColorTuple dataTuple)
    {
        return (
            (uint)_dogComparer.GetHashCode(dataTuple.Dog),
            (uint)_stringComparer.GetHashCode(dataTuple.Place),
            (uint)_colorComparer.GetHashCode(dataTuple.Color)
        );
    }
}