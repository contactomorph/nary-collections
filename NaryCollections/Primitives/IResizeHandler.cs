namespace NaryCollections.Primitives;

public interface IResizeHandler<in TDataEntry, TBackIndex> where TBackIndex : struct
{
    uint GetHashCodeAt(TDataEntry[] dataTable, int index);
    TBackIndex GetBackIndex(TDataEntry[] dataTable, int index);
    void SetBackIndex(TDataEntry[] dataTable, int index, TBackIndex backIndex);
}