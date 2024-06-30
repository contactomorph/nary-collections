namespace NaryCollections.Primitives;

public interface IDataProjector<in TDataEntry, in T> :
    IDataEquator<TDataEntry, T>,
    IResizeHandler<TDataEntry>,
    IItemHasher<T>
{
}