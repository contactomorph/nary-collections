#if !NET6_0_OR_GREATER
using System.Collections;

namespace NaryMaps;

public readonly struct ImmutableArray<T>(IEnumerable<T> items) : IReadOnlyList<T>, IEquatable<ImmutableArray<T>>
{
    private readonly T[]? _array = items.ToArray();
    
    public int Count => _array?.Length ?? 0;
    
    public int Length => _array?.Length ?? 0;

    public IEnumerator<T> GetEnumerator()
    {
        var enumerable = _array?.AsEnumerable() ?? [];
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T this[int index]
    {
        get
        {
            if (ReferenceEquals(_array, null) || index < 0 || index >= _array.Length)
                throw new ArgumentOutOfRangeException();
            return _array[index];
        }
    }

    public bool Equals(ImmutableArray<T> other)
    {
        if (ReferenceEquals(_array, other._array))
            return true;
        if (ReferenceEquals(_array, null) || ReferenceEquals(other._array, null))
            return false;
        if (_array.Length != other._array.Length)
            return false;
        for (var i = 0; i < _array.Length; i++)
            if (!Equals(_array[i], other._array[i]))
                return false;
        return true;
    }
    
    public override bool Equals(object? obj) => obj is ImmutableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array is null) return 0;
        unchecked
        {
            int hash = _array.Length * 23;
            foreach (var item in _array)
                hash = hash * 23 + item?.GetHashCode() ?? 0;
            return hash;
        }
    }

    public override string ToString()
    {
        return $"Count: {Count}";
    }
}

public static class ImmutableArray
{
    public static ImmutableArray<T> ToImmutableArray<T>(this IEnumerable<T> items) => new ImmutableArray<T>(items);

    public static ImmutableArray<T> Create<T>(params T[] items)
    {
        return new ImmutableArray<T>(items.ToArray());
    }
}

#endif