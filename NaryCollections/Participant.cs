namespace NaryCollections;

public interface IParticipant
{
    object Schema { get; }
    byte Index { get; }
    bool Unique { get; }
    Type ItemType { get; }
}

public class Participant<T> : IParticipant
{
    public object Schema { get; }
    public byte Index { get; }

    public virtual bool Unique => false;
    public Type ItemType => typeof(T);

    internal Participant(object schema, byte index)
    {
        this.Schema = schema;
        Index = index;
    }
}

#pragma warning disable CS0660, CS0661
public class SearchableParticipant<T> : Participant<T>
#pragma warning restore CS0660, CS0661
{
    public override bool Unique { get; }

    internal SearchableParticipant(object schema, byte index, bool unique) : base(schema, index)
    {
        Unique = unique;
    }
}

public sealed class OrderedParticipant<T> : SearchableParticipant<T>
{
    internal OrderedParticipant(object schema, byte index, bool unique) : base(schema, index, unique)
    {
        
    }
}