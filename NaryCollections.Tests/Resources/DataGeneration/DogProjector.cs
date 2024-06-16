using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

internal sealed class DogProjector(IEqualityComparer<Dog>? comparer = null) : IDataProjector<DogPlaceColorEntry, Dog>
{
    public static readonly DogProjector Instance = new();
    
    private readonly IEqualityComparer<Dog> _comparer = comparer ?? EqualityComparer<Dog>.Default;

    public (Dog Item, uint HashCode) GetDataAt(DogPlaceColorEntry[] dataTable, int index)
    {
        return (dataTable[index].DataTuple.Dog, dataTable[index].HashTuple.Item1);
    }

    public bool AreDataEqualAt(DogPlaceColorEntry[] dataTable, int index, Dog item, uint hashCode)
    {
        return dataTable[index].HashTuple.Item1 == hashCode &&
               _comparer.Equals(dataTable[index].DataTuple.Dog, item);
    }

    public int GetBackIndex(DogPlaceColorEntry[] dataTable, int index) => dataTable[index].BackIndexesTuple.Item1;

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index, int backIndex)
    {
        dataTable[index].BackIndexesTuple.Item1 = backIndex;
    }

    public uint ComputeHashCode(Dog item) => (uint)_comparer.GetHashCode(item);
}