namespace NaryMaps;

public interface IParticipant
{
    object Schema { get; }
    Type ItemType { get; }
    byte Rank { get; }
    bool IsUnique { get; }
    bool IsSearchable { get; }
    bool IsOrdered { get; }
}

public abstract class ParticipantBase<T> : IParticipant
{
    public object Schema { get; }
    public Type ItemType { get; }
    
    public byte Rank { get; }
    
    public abstract bool IsUnique { get; }
    public abstract bool IsSearchable { get; }
    public abstract bool IsOrdered { get; }

    private protected ParticipantBase(object schema, byte rank)
    {
        Schema = schema;
        ItemType = typeof(T);
        Rank = rank;
    }
}

public abstract class ParticipantBase<TK, T> : ParticipantBase<T> where TK : CompositeKind.Basic
{
    public override bool IsUnique { get; }
    public override bool IsSearchable { get; }
    public override bool IsOrdered { get; }

    private protected ParticipantBase(object schema, byte rank) : base(schema, rank)
    {
        var kind = typeof(TK);
        IsUnique = kind == typeof(CompositeKind.UniqueSearchable) || kind == typeof(CompositeKind.UniqueOrdered);
        IsOrdered = kind == typeof(CompositeKind.Ordered) || kind == typeof(CompositeKind.UniqueOrdered);
        IsSearchable = kind != typeof(CompositeKind.Basic);
    }
}

public sealed class Participant<T> : ParticipantBase<CompositeKind.Basic, T>
{
    internal Participant(object schema) : base(schema, byte.MaxValue) { }
}

public class SearchableParticipant<T> : ParticipantBase<CompositeKind.Searchable, T>
{
    internal SearchableParticipant(object schema, byte rank) : base(schema, rank) { }
}

public class UniqueSearchableParticipant<T> : ParticipantBase<CompositeKind.UniqueSearchable, T>
{
    internal UniqueSearchableParticipant(object schema, byte rank) : base(schema, rank) {}
}
