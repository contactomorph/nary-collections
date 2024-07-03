using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections;

public static class NaryCollection
{
    public static INaryCollection<TSchema> New<TSchema>() where TSchema : Schema, new()
    {
        throw new NotImplementedException();
    }

    public static IConflictingSet<TDataTuple> AsSet<TDataTuple>(
        this INaryCollection<Schema<TDataTuple>> collection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        return (IConflictingSet<TDataTuple>)collection;
    }
    
    public static IReadOnlyConflictingSet<TDataTuple> AsReadOnlySet<TDataTuple>(
        this IReadOnlyNaryCollection<Schema<TDataTuple>> collection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        return (IReadOnlyConflictingSet<TDataTuple>)collection;
    }
}