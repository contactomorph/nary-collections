using NaryMaps.Primitives;

namespace NaryMaps.Fakes;

public struct FakeHashTableProvider : IDataEquator<ValueTuple, ValueTuple, object>, IHashTableProvider
{
    public HashEntry[] GetHashTable() => throw new NotImplementedException();

    public int GetHashEntryCount() => throw new NotImplementedException();
    
    public bool AreDataEqualAt(ValueTuple[] dataTable, ValueTuple comparerTuple, int index, object item, uint hashCode)
    {
        throw new NotImplementedException();
    }
}