using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections;

public static class NaryCollection
{
    public static INaryCollection<TSchema> New<TSchema>() where TSchema : Schema, new()
    {
        throw new NotImplementedException();
    }

    public static ISet<TDataTuple> AsSet<TDataTuple>(
        this INaryCollection<Schema<TDataTuple>> collection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        return (ISet<TDataTuple>)collection;
    }
    
    public static IReadOnlySet<TDataTuple> AsReadOnlySet<TDataTuple>(
        this IReadOnlyNaryCollection<Schema<TDataTuple>> collection)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        return (IReadOnlySet<TDataTuple>)collection;
    }
}