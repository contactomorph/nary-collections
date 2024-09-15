namespace NaryMaps;

public interface IReadOnlyNaryMap<out TSchema> where TSchema : Schema
{
    public TSchema Schema { get; }
    
    public IReadOnlySet<T> AsReadOnlySet<TK, T>(Func<TSchema, ParticipantBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable;
    
    public IReadOnlySet<T> AsReadOnlySet<TK, T>(Func<TSchema, CompositeBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable;

    public ISelection<TSchema, TK, T> With<TK, T>(Func<TSchema, ParticipantBase<TK, T>> selector)
        where TK : CompositeKind.Basic;
    
    public ISelection<TSchema, TK, T> With<TK, T>(Func<TSchema, CompositeBase<TK, T>> selector)
        where TK : CompositeKind.Basic, CompositeKind.ISearchable;
}

public interface INaryMap<out TSchema> : IReadOnlyNaryMap<TSchema> where TSchema : Schema
{
}