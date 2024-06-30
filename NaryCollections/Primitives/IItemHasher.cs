namespace NaryCollections.Primitives;

public interface IItemHasher<in T>
{
    uint ComputeHashCode(T item);
}