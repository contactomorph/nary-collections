namespace NaryMaps;

public interface IReadOnlyNaryMap<out TSchema> where TSchema : Schema
{
    public TSchema Schema { get; }
    
    public abstract IRelationSelection<TSchema, T> With<T>(Func<TSchema, SearchableComposite<T>> selector);

    public abstract IRelationSelection<TSchema, T> With<T>(Func<TSchema, SearchableParticipant<T>> selector);

    public abstract IOrderedRelationSelection<TSchema, T> With<T>(Func<TSchema, OrderedComposite<T>> selector);

    public abstract IOrderedRelationSelection<TSchema, T> With<T>(Func<TSchema, OrderedParticipant<T>> selector);
}

public interface INaryMap<out TSchema> : IReadOnlyNaryMap<TSchema> where TSchema : Schema
{
}