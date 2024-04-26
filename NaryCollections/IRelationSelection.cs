namespace NaryCollections;

public interface IRelationSelection<out TSchema, in T>
{
    IRelationSlicing<TSchema> Among(params T[] values);
    
    IRelationSlicing<TSchema> Among(IEnumerable<T> values);
}