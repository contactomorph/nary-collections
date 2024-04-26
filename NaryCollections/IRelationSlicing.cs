using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections;

public interface IRelationSlicing<out TSchema>
{
    IEnumerable<TArgTuple> Get<TArgTuple>(Func<TSchema, Schema<TArgTuple>> selector)
        where TArgTuple : struct, ITuple, IStructuralEquatable;

    IEnumerable<T> Get<T>(Func<TSchema, Participant<T>> selector);

    T? GetFirstOrDefault<T>(Func<TSchema, Participant<T>> selector, T? defaultValue = default);

    T? GetFirstOrNull<T>(Func<TSchema, Participant<T>> selector) where T : struct;
}