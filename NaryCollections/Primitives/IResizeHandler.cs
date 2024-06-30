namespace NaryCollections.Primitives;

public interface IResizeHandler<in TDataEntry>
{
    uint GetHashCodeAt(TDataEntry[] dataTable, int index);
    int GetBackIndex(TDataEntry[] dataTable, int index);
    void SetBackIndex(TDataEntry[] dataTable, int index, int backIndex);
}