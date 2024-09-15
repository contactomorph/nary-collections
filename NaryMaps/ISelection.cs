using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace NaryMaps;

public interface ISelection<out TSchema, TK, in T> where TK : CompositeKind.Basic { }

public static class Selection
{
    [Pure]
    public static IReadOnlyDictionary<T, IEnumerable<TDataTuple>> AsReadOnlyDictionaryOfEnumerable<TDataTuple, TK, T>(
        this ISelection<Schema<TDataTuple>, TK, T> selection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
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
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (selection is IReadOnlyDictionary<T, TDataTuple> dictionary)
            return dictionary;
        throw new InvalidOperationException("Unexpected selection type.");
    }
}
