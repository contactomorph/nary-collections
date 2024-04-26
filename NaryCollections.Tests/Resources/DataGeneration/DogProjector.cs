using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;
using DogPlaceColorTuple = (NaryCollections.Tests.Resources.Types.Dog Dog, string Place, System.Drawing.Color Color);
using HashTuple = (uint, uint, uint);

namespace NaryCollections.Tests.Resources.DataGeneration;

internal sealed class DogProjector(DataEntry<DogPlaceColorTuple, HashTuple, ValueTuple<int>>[] dataTable)
    : IDataProjector<Dog>
{
    private readonly IEqualityComparer<Dog> _comparer = EqualityComparer<Dog>.Default;

    public (Dog Item, uint HashCode) GetDataAt(int index)
    {
        return (dataTable[index].DataTuple.Dog, dataTable[index].HashTuple.Item1);
    }

    public bool AreDataEqualAt(int index, Dog item, uint hashCode)
    {
        return dataTable[index].HashTuple.Item1 == hashCode &&
               _comparer.Equals(dataTable[index].DataTuple.Dog, item);
    }

    public void SetDataAt(int index, Dog item, uint hashCode)
    {
        dataTable[index].DataTuple.Dog = item;
        dataTable[index].HashTuple.Item1 = hashCode;
    }

    public int GetBackIndex(int index)
    {
        throw new NotImplementedException();
    }

    public void SetBackIndex(int index)
    {
        throw new NotImplementedException();
    }
}