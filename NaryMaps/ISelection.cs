using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace NaryMaps;

public interface ISelection<out TSchema, TK, in T> where TK : CompositeKind.Basic { }

public static class Selection
{
    [Pure]
    public static IReadOnlyDictionary<T, IEnumerable<TDataTuple>> AsReadOnlyMultiDictionary<TDataTuple, TK, T>(
        this ISelection<Schema<TDataTuple>, TK, T> selection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if NETCOREAPP3_1
        where T : notnull
#endif
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (selection is IReadOnlyDictionary<T, IEnumerable<TDataTuple>> dictionary)
            return dictionary;
        throw new InvalidOperationException("Unexpected selection type.");
    }
    
    [Pure]
    public static IReadOnlyDictionary<T, TDataTuple> AsReadOnlyDictionary<TDataTuple, TK, T>(
        this ISelection<Schema<TDataTuple>, TK, T> selection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable, CompositeKind.IUnique
#if NETCOREAPP3_1
        where T : notnull
#endif
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (selection is IReadOnlyDictionary<T, TDataTuple> dictionary)
            return dictionary;
        throw new InvalidOperationException("Unexpected selection type.");
    }
    
    [Pure]
    public static IEnumerable<TDataTuple> Among<TDataTuple, TK, T>(
        this ISelection<Schema<TDataTuple>, TK, T> selection,
        params T[] values)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
    {
        throw new NotImplementedException();
    }
    
    [Pure]
    public static IEnumerable<TDataTuple> Among<TDataTuple, TK, T>(
        this ISelection<Schema<TDataTuple>, TK, T> selection,
        IEnumerable<T> values)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
    {
        throw new NotImplementedException();
    }
    
    [Pure]
    public static IEnumerable<TDataTuple> AscendingFrom<TDataTuple, TK, T>(
        this ISelection<Schema<TDataTuple>, TK, T> selection,
        T min)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.IOrdered
    {
        throw new NotImplementedException();
    }
    
    [Pure]
    public static IEnumerable<TDataTuple> DescendingFrom<TDataTuple, TK, T>(
        this ISelection<Schema<TDataTuple>, TK, T> selection,
        T max)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.IOrdered
    {
        throw new NotImplementedException();
    }
}
