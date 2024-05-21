using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.DataGeneration;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

internal sealed class DogPlaceColorProjector
    : IDataProjector<DogPlaceColorEntry, (Dog Dog, string Place, Color Color)>
{
    private readonly IEqualityComparer<DogPlaceColorTuple> _comparer = EqualityComparer<DogPlaceColorTuple>.Default;

    public (DogPlaceColorTuple Item, uint HashCode) GetDataAt(DogPlaceColorEntry[] dataTable, int index)
    {
        throw new NotImplementedException();
    }

    public bool AreDataEqualAt(DogPlaceColorEntry[] dataTable, int index, DogPlaceColorTuple item, uint hashCode)
    {
        return (uint)dataTable[index].HashTuple.GetHashCode() == hashCode &&
               _comparer.Equals(dataTable[index].DataTuple, item);
    }

    public void SetDataAt(DogPlaceColorEntry[] dataTable, int index, DogPlaceColorTuple item, uint hashCode)
    {
        throw new NotImplementedException();
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