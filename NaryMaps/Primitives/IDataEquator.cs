namespace NaryMaps.Primitives;

public interface IDataEquator<in TDataEntry, in TComparerTuple, in T>
{
    bool AreDataEqualAt(TDataEntry[] dataTable, TComparerTuple comparerTuple, int index, T item, uint hashCode);
}