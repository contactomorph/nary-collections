namespace NaryCollections.Primitives;

public interface IItemHasher<in TComparerTuple, in T>
{
    uint ComputeHashCode(TComparerTuple comparerTuple, T item);
}