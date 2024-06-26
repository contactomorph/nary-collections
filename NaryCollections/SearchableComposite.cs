using System.Collections.Immutable;

namespace NaryCollections;

public class SearchableComposite<TCompositeTuple>
{
    internal byte Rank { get; }
    internal ImmutableArray<IParticipant> Participants { get; }

    internal SearchableComposite(byte rank, ImmutableArray<IParticipant> participants)
    {
        Rank = rank;
        Participants = participants;
    }
}