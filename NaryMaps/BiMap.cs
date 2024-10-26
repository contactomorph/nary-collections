using System.Diagnostics.Contracts;

namespace NaryMaps;

public static class BiMap
{
    public static INaryMap<Schema<TDirect, TInverse>> New<TDirect, TInverse>()
    {
        return NaryMap.New<Schema<TDirect, TInverse>>();
    }
    
    [Pure]
    public static IReadOnlyDictionary<TDirect, TInverse> AsDirectReadOnlyDictionary<TDirect, TInverse>(
        this IReadOnlyNaryMap<Schema<TDirect, TInverse>> map)
#if !NET6_0_OR_GREATER
        where TDirect : notnull
#endif
    {
        return map.With(s => s.Direct).AsReadOnlyDictionary(p => p.Item2);
    }
    
    [Pure]
    public static IRemoveOnlyDictionary<TDirect, TInverse> AsDirectDictionary<TDirect, TInverse>(
        this INaryMap<Schema<TDirect, TInverse>> map)
#if !NET6_0_OR_GREATER
        where TDirect : notnull
#endif
    {
        return map.With(s => s.Direct).AsDictionary(p => p.Item2);
    }
    
    [Pure]
    public static IReadOnlyDictionary<TInverse, TDirect> AsInverseReadOnlyDictionary<TDirect, TInverse>(
        this IReadOnlyNaryMap<Schema<TDirect, TInverse>> map)
#if !NET6_0_OR_GREATER
        where TInverse : notnull
#endif
    {
        return map.With(s => s.Inverse).AsReadOnlyDictionary(p => p.Item1);
    }
    
    [Pure]
    public static IRemoveOnlyDictionary<TInverse, TDirect> AsInverseDictionary<TDirect, TInverse>(
        this INaryMap<Schema<TDirect, TInverse>> map)
#if !NET6_0_OR_GREATER
        where TInverse : notnull
#endif
    {
        return map.With(s => s.Inverse).AsDictionary(p => p.Item1);
    }
    
    public sealed class Schema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
    {
        public UniqueSearchableParticipant<TInverse> Inverse { get;  }

        public UniqueSearchableParticipant<TDirect> Direct { get; }
    
        public Schema()
        {
            Direct = DeclareUniqueSearchableParticipant<TDirect>();
            Inverse = DeclareUniqueSearchableParticipant<TInverse>();
            Sign = Conclude(Direct, Inverse);
        }

        protected override Signature Sign { get; }
    }
}