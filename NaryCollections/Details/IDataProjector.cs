namespace NaryCollections.Details;

internal interface IDataProjector<T>
{
    (T Item, uint HashCode) GetDataAt(int index);
    bool AreDataEqualAt(int index, T item, uint hashCode);
    void SetDataAt(int index, T item, uint hashCode);
    int GetBackIndex(int index);
    void SetBackIndex(int index);
}
