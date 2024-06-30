namespace NaryCollections.Primitives;

public interface IDataProjector<in TDataEntry, T>
{
    (T Item, uint HashCode) GetDataAt(TDataEntry[] dataTable, int index);
    bool AreDataEqualAt(TDataEntry[] dataTable, int index, T item, uint hashCode);
    int GetBackIndex(TDataEntry[] dataTable, int index);
    void SetBackIndex(TDataEntry[] dataTable, int index, int backIndex);
    uint ComputeHashCode(T item);
}