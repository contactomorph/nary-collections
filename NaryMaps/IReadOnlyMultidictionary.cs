using System.Diagnostics.CodeAnalysis;

namespace NaryMaps;

public interface IReadOnlyMultiDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
{
    public IEnumerable<TKey> Keys { get; }
    public IEnumerable<TValue> Values { get; }
    public IEnumerable<TValue> this[TKey key] { get; }
    public bool ContainsKey(TKey key);
    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IEnumerable<TValue> values);
}