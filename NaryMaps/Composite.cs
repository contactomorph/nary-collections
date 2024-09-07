using System.Collections.Immutable;

namespace NaryMaps;

public abstract class CompositeBase<TK, TDataTuple> where TK : CompositeKind.Basic, CompositeKind.ISearchable
{
    internal object Schema { get; }
    internal byte Rank { get; }
    internal ImmutableArray<IParticipant> Participants { get; }

    private protected CompositeBase(object schema, byte rank, ImmutableArray<IParticipant> participants)
    {
        Schema = schema;
        Rank = rank;
        Participants = participants;
    }
}

public sealed class Composite<TDataTuple> : CompositeBase<CompositeKind.Searchable, TDataTuple>
{
    internal Composite(object schema, byte rank, ImmutableArray<IParticipant> participants) :
        base(schema, rank, participants) { }
}

public sealed class UniqueComposite<TDataTuple> : CompositeBase<CompositeKind.UniqueSearchable, TDataTuple>
{
    internal UniqueComposite(object schema, byte rank, ImmutableArray<IParticipant> participants) :
        base(schema, rank, participants) { }
}
