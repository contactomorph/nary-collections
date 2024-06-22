using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections;

public static class NaryCollection
{
    public static INaryCollection<TSchema> New<TSchema>() where TSchema : Schema, new()
    {
        throw new NotImplementedException();
    }

    public static ISet<TArgTuple> AsSet<TArgTuple>(
        this INaryCollection<Schema<TArgTuple>> collection)
        where TArgTuple : struct, ITuple, IStructuralEquatable
    {
        return (ISet<TArgTuple>)collection;
    }
    
    public static IReadOnlySet<TArgTuple> AsReadOnlySet<TArgTuple>(
        this IReadOnlyNaryCollection<Schema<TArgTuple>> collection)
        where TArgTuple : struct, ITuple, IStructuralEquatable
    {
        return (IReadOnlySet<TArgTuple>)collection;
    }
}