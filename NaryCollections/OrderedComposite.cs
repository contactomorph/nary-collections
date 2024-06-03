using System.Collections.Immutable;

namespace NaryCollections;

public sealed class OrderedComposite<TCompositeTuple> : SearchableComposite<TCompositeTuple>
{
    internal OrderedComposite(byte rank, ImmutableArray<IParticipant> participants) : base(rank, participants)
    {}
}