using System.Drawing;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Types;
using DogPlaceColorTuple = (NaryCollections.Tests.Resources.Types.Dog Dog, string Place, System.Drawing.Color Color);
using HashTuple = (uint, uint, uint);

namespace NaryCollections.Tests.Resources.DataGeneration;

internal sealed class DogPlaceColorProjector(DataEntry<DogPlaceColorTuple, HashTuple, ValueTuple<int>>[] dataTable)
    : IDataProjector<(Dog Dog, string Place, Color Color)>
{
    private readonly IEqualityComparer<DogPlaceColorTuple> _comparer = EqualityComparer<DogPlaceColorTuple>.Default;

    public (DogPlaceColorTuple Item, uint HashCode) GetDataAt(int index)
    {
        throw new NotImplementedException();
    }

    public bool AreDataEqualAt(int index, DogPlaceColorTuple item, uint hashCode)
    {
        return (uint)dataTable[index].HashTuple.GetHashCode() == hashCode &&
               _comparer.Equals(dataTable[index].DataTuple, item);
    }

    public void SetDataAt(int index, DogPlaceColorTuple item, uint hashCode)
    {
        throw new NotImplementedException();
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