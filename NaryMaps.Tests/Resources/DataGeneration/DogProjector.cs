using System.Drawing;
using NaryMaps.Primitives;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests.Resources.DataGeneration;

using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

internal readonly struct DogProjector :
    IDataEquator<DogPlaceColorEntry, ComparerTuple, Dog>,
    IResizeHandler<DogPlaceColorEntry, int>,
    IResizeHandler<DogPlaceColorEntry, MultiIndex>
{
    public static readonly DogProjector Instance = new();

    public uint GetHashCodeAt(DogPlaceColorEntry[] dataTable, int index) => dataTable[index].HashTuple.Item1;

    public bool AreDataEqualAt(
        DogPlaceColorEntry[] dataTable,
        ComparerTuple comparerTuple,
        int index,
        Dog item,
        uint hashCode)
    {
        return dataTable[index].HashTuple.Item1 == hashCode &&
               comparerTuple.Item1.Equals(dataTable[index].DataTuple.Dog, item);
    }

    public int GetBackIndex(DogPlaceColorEntry[] dataTable, int index) => dataTable[index].BackIndexesTuple.Item1;

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, int backIndex)
    {
        dataTable[index].BackIndexesTuple.Item1 = backIndex;
    }

    MultiIndex IResizeHandler<DogPlaceColorEntry, MultiIndex>.GetBackIndex(DogPlaceColorEntry[] dataTable, int index)
    {
        return dataTable[index].BackIndexesTuple.Item2;
    }

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, MultiIndex backIndex)
    {
        dataTable[index].BackIndexesTuple.Item2 = backIndex;
    }
}