namespace NaryCollections;

public interface IReadOnlyNaryCollection<out TSchema> where TSchema : Schema
{
    public TSchema Schema { get; }
    public IReadOnlyCollection<object?> GetUntypedSet();
}

public interface INaryCollection<out TSchema> : IReadOnlyNaryCollection<TSchema> where TSchema : Schema
{
}