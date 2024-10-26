using System.Diagnostics.Contracts;

namespace NaryMaps;

public static class MultiMap
{
    public static INaryMap<Schema<TKey, TValue>> New<TKey, TValue>()
    {
        return NaryMap.New<Schema<TKey, TValue>>();
    }
    
    [Pure]
    public static IReadOnlyDictionary<TKey, IEnumerable<TValue>> AsReadOnlyDictionaryOfEnumerable<TKey, TValue>(
        this IReadOnlyNaryMap<Schema<TKey, TValue>> map)
#if !NET6_0_OR_GREATER
        where TKey : notnull
#endif
    {
        return map.With(s => s.Key).AsReadOnlyDictionaryOfEnumerable(p => p.Item2);
    }
    
    [Pure]
    public static IRemoveOnlyDictionary<TKey, IEnumerable<TValue>> AsDictionaryOfEnumerable<TKey, TValue>(
        this INaryMap<Schema<TKey, TValue>> map)
#if !NET6_0_OR_GREATER
        where TKey : notnull
#endif
    {
        return map.With(s => s.Key).AsDictionaryOfEnumerable(p => p.Item2);
    }
    
    public sealed class Schema<TKey, TValue> : Schema<(TKey, TValue)>
    {
        public SearchableParticipant<TKey> Key { get; }
        public Participant<TValue> Value { get;  }
    
        public Schema()
        {
            Key = DeclareSearchableParticipant<TKey>();
            Value = DeclareParticipant<TValue>();
            Sign = Conclude(Key, Value);
        }

        protected override Signature Sign { get; }
    }
}
