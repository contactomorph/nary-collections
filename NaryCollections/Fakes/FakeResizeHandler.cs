using NaryCollections.Primitives;

namespace NaryCollections.Fakes;

public struct FakeResizeHandler : IResizeHandler<ValueTuple, int>, IResizeHandler<ValueTuple, MultiIndex>
{
    public uint GetHashCodeAt(ValueTuple[] dataTable, int index) => throw new NotImplementedException();

    public int GetBackIndex(ValueTuple[] dataTable, int index) => throw new NotImplementedException();
        
    MultiIndex IResizeHandler<ValueTuple, MultiIndex>.GetBackIndex(ValueTuple[] dataTable, int index)
    {
        throw new NotImplementedException();
    }

    public void SetBackIndex(ValueTuple[] dataTable, int index, int backIndex)
    {
        throw new NotImplementedException();
    }

    public void SetBackIndex(ValueTuple[] dataTable, int index, MultiIndex backIndex)
    {
        throw new NotImplementedException();
    }
}