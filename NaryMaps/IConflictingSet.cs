namespace NaryMaps;

public interface IConflictingSet<T> : IReadOnlySet<T>, ISet<T>
{
    new int Count { get; }
    
    new bool Contains(T item);
    
    new bool IsProperSubsetOf(IEnumerable<T> other);
    
    new bool IsProperSupersetOf(IEnumerable<T> other);
    
    new bool IsSubsetOf(IEnumerable<T> other);
    
    new bool IsSupersetOf(IEnumerable<T> other);
    
    new bool Overlaps(IEnumerable<T> other);
    
    new bool SetEquals(IEnumerable<T> other);
}