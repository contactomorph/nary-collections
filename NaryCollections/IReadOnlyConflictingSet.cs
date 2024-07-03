namespace NaryCollections;

public interface IReadOnlyConflictingSet<T> : IReadOnlySet<T>
{
    bool IsConflictingWith(T item);
}