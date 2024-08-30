namespace NaryMaps;

public interface IOrderedRelationSelection<out TSchema, in T> : IRelationSelection<TSchema, T>
{
    IRelationSlicing<TSchema> Between(T min, T max, bool minIncluded = true, bool maxIncluded = false);
    IRelationSlicing<TSchema> JustAfter(T min);
    IRelationSlicing<TSchema> EqualOrJustAfter(T min);
    IRelationSlicing<TSchema> JustBefore(T max);
    IRelationSlicing<TSchema> EqualOrJustBefore(T max);
}