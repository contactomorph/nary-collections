using NaryMaps.Primitives;

namespace NaryMaps.Fakes;

public readonly struct FakeDataEquator : IDataEquator<ValueTuple, ValueTuple, object>
{
    public bool AreDataEqualAt(ValueTuple[] dataTable, ValueTuple comparerTuple, int index, object item, uint hashCode)
    {
        throw new NotImplementedException();
    }
}