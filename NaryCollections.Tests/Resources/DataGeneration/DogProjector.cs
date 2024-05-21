using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

internal sealed class DogProjector : IDataProjector<DogPlaceColorEntry, Dog>
{
    private readonly IEqualityComparer<Dog> _comparer = EqualityComparer<Dog>.Default;

    public (Dog Item, uint HashCode) GetDataAt(DogPlaceColorEntry[] dataTable, int index)
    {
        return (dataTable[index].DataTuple.Dog, dataTable[index].HashTuple.Item1);
    }

    public bool AreDataEqualAt(DogPlaceColorEntry[] dataTable, int index, Dog item, uint hashCode)
    {
        return dataTable[index].HashTuple.Item1 == hashCode &&
               _comparer.Equals(dataTable[index].DataTuple.Dog, item);
    }

    public void SetDataAt(DogPlaceColorEntry[] dataTable, int index, Dog item, uint hashCode)
    {
        dataTable[index].DataTuple.Dog = item;
        dataTable[index].HashTuple.Item1 = hashCode;
    }

    public int GetBackIndex(DogPlaceColorEntry[] dataTable, int index)
    {
        throw new NotImplementedException();
    }

    public void SetBackIndex(DogPlaceColorEntry[] dataTable, int index)
    {
        throw new NotImplementedException();
    }
}