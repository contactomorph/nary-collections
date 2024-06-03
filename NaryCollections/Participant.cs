namespace NaryCollections;

public interface IParticipant
{
    object Schema { get; }
    bool Unique { get; }
    Type ItemType { get; }
}

public class Participant<T> : IParticipant
{
    public object Schema { get; }

    public bool Unique { get; }
    public Type ItemType => typeof(T);

    protected Participant(object schema, bool unique)
    {
        Schema = schema;
        Unique = unique;
    }
    
    internal Participant(object schema)
    {
        Schema = schema;
        Unique = false;
    }
}

#pragma warning disable CS0660, CS0661
public class SearchableParticipant<T> : Participant<T>
#pragma warning restore CS0660, CS0661
{
    public byte Rank { get; }
    
    internal SearchableParticipant(object schema, byte rank, bool unique) : base(schema, unique)
    {
        Rank = rank;
    }
}

public sealed class OrderedParticipant<T> : SearchableParticipant<T>
{
    internal OrderedParticipant(object schema, byte rank, bool unique) : base(schema, rank, unique)
    {
    }
}