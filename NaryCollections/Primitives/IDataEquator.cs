namespace NaryCollections.Primitives;

public interface IDataEquator<in TDataEntry, in T>
{
    bool AreDataEqualAt(TDataEntry[] dataTable, int index, T item, uint hashCode);
}

public interface IDataEquator<in TDataEntry, in TComparerTuple, in T>
{
    bool AreDataEqualAt(TDataEntry[] dataTable, TComparerTuple comparerTuple, int index, T item, uint hashCode);
}