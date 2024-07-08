using System.Drawing;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

internal readonly struct ColorProjector :
    IResizeHandler<DogPlaceColorEntry, MultiIndex>,
    IDataEquator<DogPlaceColorEntry, ComparerTuple, Color>
{
    public static readonly ColorProjector Instance = new();

    public uint GetHashCodeAt(DogPlaceColorEntry[] dataTable, int index)
    {
        return dataTable[index].HashTuple.Item3;
    }

    public MultiIndex GetBackIndex(DogPlaceColorEntry[] dataTable, int index)
    {
        return dataTable[index].BackIndexesTuple.Item2;
    }

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, MultiIndex backIndex)
    {
        dataTable[index].BackIndexesTuple.Item2 = backIndex;
    }

    public bool AreDataEqualAt(
        DogPlaceColorEntry[] dataTable,
        ComparerTuple comparerTuple,
        int index,
        Color item,
        uint hashCode)
    {
        return dataTable[index].HashTuple.Item3 == hashCode &&
               comparerTuple.Item3.Equals(dataTable[index].DataTuple.Color, item);
    }
}