using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections;

public abstract class NaryCollection<TSchema> : INaryCollection<TSchema> where TSchema : Schema, new()
{
    public TSchema Schema => throw new NotImplementedException();
    
    public abstract IReadOnlyCollection<object?> GetUntypedSet();

    public static NaryCollection<TSchema> New() => throw new NotImplementedException();

    public abstract IRelationSelection<TSchema, T> With<T>(Func<TSchema, SearchableComposite<T>> selector);

    public abstract IRelationSelection<TSchema, T> With<T>(Func<TSchema, SearchableParticipant<T>> selector);

    public abstract IOrderedRelationSelection<TSchema, T> With<T>(Func<TSchema, OrderedComposite<T>> selector);

    public abstract IOrderedRelationSelection<TSchema, T> With<T>(Func<TSchema, OrderedParticipant<T>> selector);
}

public static class NaryCollection
{
    public static IReadOnlySet<TArgTuple> AsSet<TArgTuple>(this IReadOnlyNaryCollection<Schema<TArgTuple>> collection)
        where TArgTuple : struct, ITuple, IStructuralEquatable
    {
        return (IReadOnlySet<TArgTuple>)collection.GetUntypedSet();
    }
}