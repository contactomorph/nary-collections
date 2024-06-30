namespace NaryCollections.Primitives;

public interface IDataProjector<in TDataEntry, in T> : IDataEquator<TDataEntry, T>, IResizeHandler<TDataEntry>
{
    uint ComputeHashCode(T item);
}

public interface IDataEquator<in TDataEntry, in T>
{
    bool AreDataEqualAt(TDataEntry[] dataTable, int index, T item, uint hashCode);
}

public interface IResizeHandler<in TDataEntry>
{
    uint GetHashCodeAt(TDataEntry[] dataTable, int index);
    int GetBackIndex(TDataEntry[] dataTable, int index);
    void SetBackIndex(TDataEntry[] dataTable, int index, int backIndex);
}