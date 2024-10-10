namespace NaryMaps;

public interface IReadOnlyConflictingSet<T> : IReadOnlySet<T>
{
    bool IsConflictingWith(T value);
    
    List<T> GetConflictingItemsWith(T value);
}