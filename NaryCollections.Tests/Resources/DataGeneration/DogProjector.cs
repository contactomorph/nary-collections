using System.Drawing;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

internal readonly struct DogProjector(IEqualityComparer<Dog>? comparer = null) :
    IDataProjector<DogPlaceColorEntry, ComparerTuple, Dog>
{
    public static readonly DogProjector Instance = new(EqualityComparer<Dog>.Default);
    
    private readonly IEqualityComparer<Dog> _comparer = comparer ?? EqualityComparer<Dog>.Default;

    public uint GetHashCodeAt(DogPlaceColorEntry[] dataTable, int index)
    {
        return dataTable[index].HashTuple.Item1;
    }

    public bool AreDataEqualAt(
        DogPlaceColorEntry[] dataTable,
        ComparerTuple comparerTuple,
        int index,
        Dog item,
        uint hashCode)
    {
        return dataTable[index].HashTuple.Item1 == hashCode &&
               _comparer.Equals(dataTable[index].DataTuple.Dog, item);
    }

    public int GetBackIndex(DogPlaceColorEntry[] dataTable, int index) => dataTable[index].BackIndexesTuple.Item1;

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, int backIndex)
    {
        dataTable[index].BackIndexesTuple.Item1 = backIndex;
    }

    public uint ComputeHashCode(ComparerTuple comparerTuple, Dog item) => (uint)_comparer.GetHashCode(item);
}