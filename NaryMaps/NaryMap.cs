using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps;

public static class NaryMap
{
    public static INaryMap<TSchema> New<TSchema>() where TSchema : Schema, new()
    {
        throw new NotImplementedException();
    }

    public static IConflictingSet<TDataTuple> AsSet<TDataTuple>(
        this INaryMap<Schema<TDataTuple>> map)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        return (IConflictingSet<TDataTuple>)map;
    }
    
    public static IReadOnlyConflictingSet<TDataTuple> AsReadOnlySet<TDataTuple>(
        this IReadOnlyNaryMap<Schema<TDataTuple>> map)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        return (IReadOnlyConflictingSet<TDataTuple>)map;
    }
}