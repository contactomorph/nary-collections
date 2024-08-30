using System.Collections.Immutable;

namespace NaryMaps;

public sealed class OrderedComposite<TCompositeTuple> : SearchableComposite<TCompositeTuple>
{
    internal OrderedComposite(byte rank, ImmutableArray<IParticipant> participants) : base(rank, participants)
    {}
}