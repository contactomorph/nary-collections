namespace NaryCollections.Primitives;

public interface IDataProjector<in TDataEntry, in TComparerTuple, in T> :
    IDataEquator<TDataEntry, TComparerTuple, T>,
    IResizeHandler<TDataEntry>,
    IItemHasher<TComparerTuple, T>
{
}