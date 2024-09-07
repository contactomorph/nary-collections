using System.Collections.Immutable;

namespace NaryMaps;

public abstract class CompositeBase<TK, TDataTuple> where TK : CompositeKind.Basic, CompositeKind.ISearchable
{
    internal byte Rank { get; }
    internal ImmutableArray<IParticipant> Participants { get; }

    private protected CompositeBase(byte rank, ImmutableArray<IParticipant> participants)
    {
        Rank = rank;
        Participants = participants;
    }
}

public sealed class Composite<TDataTuple> : CompositeBase<CompositeKind.Searchable, TDataTuple>
{
    internal Composite(byte rank, ImmutableArray<IParticipant> participants) : base(rank, participants) { }
}

public sealed class UniqueComposite<TDataTuple> : CompositeBase<CompositeKind.UniqueSearchable, TDataTuple>
{
    internal UniqueComposite(byte rank, ImmutableArray<IParticipant> participants) : base(rank, participants) { }
}
