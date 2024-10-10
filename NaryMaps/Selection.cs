using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using NaryMaps.Implementation;

namespace NaryMaps;

public interface IReadOnlySelection<out TSchema, TK, in T> where TK : CompositeKind.Basic { }

public interface ISelection<out TSchema, TK, in T> : IReadOnlySelection<TSchema, TK, T> where TK : CompositeKind.Basic { }

public static class Selection
{
    [Pure]
    public static IReadOnlySet<T> AsReadOnlySet<TDataTuple, TK, T>(
        this IReadOnlySelection<Schema<TDataTuple>, TK, T> selection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selection is SelectionBase<TDataTuple, T> selectionBase)
            return new ReadOnlySet<TDataTuple, T>(selectionBase);
        throw new InvalidOperationException("Unexpected selection type.");
    }
    
    [Pure]
    public static IReadOnlyDictionary<T, IEnumerable<TDataTuple>> AsReadOnlyDictionaryOfEnumerable<TDataTuple, TK, T>(
        this IReadOnlySelection<Schema<TDataTuple>, TK, T> selection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selection is SelectionBase<TDataTuple, T> selectionBase)
            return new ReadOnlyDictionaryOfEnumerable<T, TDataTuple>(selectionBase);
        throw new InvalidOperationException("Unexpected selection type.");
    }
    
    [Pure]
    public static IReadOnlyDictionary<T, IEnumerable<TValue>> AsReadOnlyDictionaryOfEnumerable<TDataTuple, TK, T, TValue>(
        this IReadOnlySelection<Schema<TDataTuple>, TK, T> selection,
        Func<TDataTuple, TValue> valueSelector)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selection is SelectionBase<TDataTuple, T> selectionBase)
            return new ReadOnlyDictionaryOfEnumerable<T, TValue, TDataTuple>(selectionBase, valueSelector);
        throw new InvalidOperationException("Unexpected selection type.");
    }

    [Pure]
    public static IReadOnlyMultiDictionary<T, TDataTuple> AsReadOnlyMultiDictionary<TDataTuple, TK, T>(
        this IReadOnlySelection<Schema<TDataTuple>, TK, T> selection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selection is SelectionBase<TDataTuple, T> selectionBase)
            return new ReadOnlyMultiDictionary<T, TDataTuple>(selectionBase);
        throw new InvalidOperationException("Unexpected selection type.");
    }
    
    [Pure]
    public static IReadOnlyMultiDictionary<T, TValue> AsReadOnlyMultiDictionary<TDataTuple, TK, T, TValue>(
        this IReadOnlySelection<Schema<TDataTuple>, TK, T> selection,
        Func<TDataTuple, TValue> valueSelector)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selection is SelectionBase<TDataTuple, T> selectionBase)
            return new ReadOnlyMultiDictionary<T, TValue, TDataTuple>(selectionBase, valueSelector);
        throw new InvalidOperationException("Unexpected selection type.");
    }
    
    [Pure]
    public static IReadOnlyDictionary<T, TDataTuple> AsReadOnlyDictionary<TDataTuple, TK, T>(
        this IReadOnlySelection<Schema<TDataTuple>, TK, T> selection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable, CompositeKind.IUnique
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selection is SelectionBase<TDataTuple, T> selectionBase)
            return new ReadOnlyDictionary<T, TDataTuple>(selectionBase);
        throw new InvalidOperationException("Unexpected selection type.");
    }
    
    [Pure]
    public static IReadOnlyDictionary<T, TValue> AsReadOnlyDictionary<TDataTuple, TK, T, TValue>(
        this IReadOnlySelection<Schema<TDataTuple>, TK, T> selection,
        Func<TDataTuple, TValue> valueSelector)
        where TDataTuple : struct, ITuple, IStructuralEquatable
        where TK : CompositeKind.Basic, CompositeKind.ISearchable, CompositeKind.IUnique
#if !NET6_0_OR_GREATER
        where T : notnull
#endif
    {
        if (selection is SelectionBase<TDataTuple, T> selectionBase)
            return new ReadOnlyDictionary<T, TValue, TDataTuple>(selectionBase, valueSelector);
        throw new InvalidOperationException("Unexpected selection type.");
    }
}
