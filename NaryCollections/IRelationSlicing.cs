using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections;

public interface IRelationSlicing<out TSchema>
{
    IEnumerable<TDataTuple> Get<TDataTuple>(Func<TSchema, Schema<TDataTuple>> selector)
        where TDataTuple : struct, ITuple, IStructuralEquatable;

    IEnumerable<T> Get<T>(Func<TSchema, Participant<T>> selector);

    T? GetFirstOrDefault<T>(Func<TSchema, Participant<T>> selector, T? defaultValue = default);

    T? GetFirstOrNull<T>(Func<TSchema, Participant<T>> selector) where T : struct;
}