using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NaryMaps.Primitives;

public static class EqualityComparerHandling
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ComputeTupleHashCode<T>(T tuple) where T : struct, ITuple, IStructuralEquatable
    {
        return (uint)tuple.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ComputeRefHashCode<T>(IEqualityComparer<T> comparer, T? item) where T : class
    {
        return item is null ? 0U : (uint)comparer.GetHashCode(item);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ComputeStructHashCode<T>(IEqualityComparer<T> comparer, T item) where T : struct
    {
        return (uint)comparer.GetHashCode(item);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ComputeEquals<T>(IEqualityComparer<T> comparer, T x, T y) => comparer.Equals(x, y);

    private static readonly MethodInfo ComputeTupleHashCodeMethodDefinition = typeof(EqualityComparerHandling)
        .GetMethod(nameof(ComputeTupleHashCode), BindingFlags.Public | BindingFlags.Static)!;
    
    private static readonly MethodInfo ComputeRefHashCodeMethodDefinition = typeof(EqualityComparerHandling)
        .GetMethod(nameof(ComputeRefHashCode), BindingFlags.Public | BindingFlags.Static)!;
    
    private static readonly MethodInfo ComputeStructHashCodeMethodDefinition = typeof(EqualityComparerHandling)
        .GetMethod(nameof(ComputeStructHashCode), BindingFlags.Public | BindingFlags.Static)!;
    
    private static readonly MethodInfo ComputeEqualsMethodDefinition = typeof(EqualityComparerHandling)
        .GetMethod(nameof(ComputeEquals), BindingFlags.Public | BindingFlags.Static)!;
    
    public static MethodInfo GetTupleHashCodeMethod(Type type)
    {
        return ComputeTupleHashCodeMethodDefinition.MakeGenericMethod(type);
    }
    
    public static MethodInfo GetItemHashCodeMethod(Type type)
    {
        return type.IsValueType ?
            ComputeStructHashCodeMethodDefinition.MakeGenericMethod(type) :
            ComputeRefHashCodeMethodDefinition.MakeGenericMethod(type);
    }
    
    public static MethodInfo GetEqualsMethod(Type type) => ComputeEqualsMethodDefinition.MakeGenericMethod(type);
}