using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace NaryCollections;

public abstract class Schema
{
    public interface ISignature
    {
        ImmutableArray<(byte Position, IParticipant Participant)> Participants { get; }
    }
    
    private protected bool Locked;
    private protected readonly List<IParticipant> Participants;
    private protected readonly List<Composite> Composites;
    private byte _rank;
    
    public abstract Type DataTupleType { get; }

    private protected Schema()
    {
        Locked = false;
        _rank = 0;
        Participants = new();
        Composites = new();
    }

    internal abstract ISignature GetSignature();
    
    protected Participant<T> AddParticipant<T>()
    {
        if (Locked)
            throw GenerateLockException();
        Participant<T> p = new(this);
        Participants.Add(p);
        return p;
    }

    protected SearchableParticipant<T> AddSearchableParticipant<T>(
        IEqualityComparer<T>? comparer = null,
        bool unique = false)
    {
        if (Locked)
            throw GenerateLockException();
        SearchableParticipant<T> p = new(this, _rank++, unique);
        Participants.Add(p);
        Composites.Add(new Composite(p.Unique, p.Rank, [p]));
        return p;
    }
    
    protected OrderedParticipant<T> AddOrderedParticipant<T>(IComparer<T> comparer, bool unique = false)
    {
        if (Locked)
            throw GenerateLockException();
        OrderedParticipant<T> p = new(this, _rank++, unique);
        Participants.Add(p);
        Composites.Add(new Composite(p.Unique, p.Rank, [p]));
        return p;
    }
    
    protected OrderedParticipant<T> AddOrderedParticipant<T>(bool unique = false) where T : IComparable<T>
    {
        if (Locked)
            throw GenerateLockException();
        OrderedParticipant<T> p = new(this, _rank++, unique);
        Participants.Add(p);
        Composites.Add(new Composite(p.Unique, p.Rank, [p]));
        return p;
    }

    protected SearchableComposite<(T1, T2)> AddSearchableComposite<T1, T2>(
        Participant<T1> indexable1,
        Participant<T2> indexable2)
    {
        if (IsInvalid(out var exception, indexable1, indexable2))
            throw exception;
        SearchableComposite<(T1, T2)> c =  new(_rank++, [indexable1, indexable2]);
        Composites.Add(new Composite(false, c.Rank, c.Participants));
        return c;
    }

    protected SearchableComposite<(T1, T2, T3)> AddSearchableInput<T1, T2, T3>(
        Participant<T1> indexable1,
        Participant<T2> indexable2,
        Participant<T3> indexable3)
    {
        if (IsInvalid(out var exception, indexable1, indexable2, indexable3))
            throw exception;
        SearchableComposite<(T1, T2, T3)> c = new(_rank++, [indexable1, indexable2, indexable3]);
        Composites.Add(new Composite(false, c.Rank, c.Participants));
        return c;
    }

    protected SearchableComposite<(T1, T2, T3, T4)> AddSearchableInput<T1, T2, T3, T4>(
        Participant<T1> indexable1,
        Participant<T2> indexable2,
        Participant<T3> indexable3,
        Participant<T4> indexable4)
    {
        if (IsInvalid(out var exception, indexable1, indexable2, indexable3, indexable4))
            throw exception;
        SearchableComposite<(T1, T2, T3, T4)> c = new(_rank++, [indexable1, indexable2, indexable3, indexable4]);
        Composites.Add(new Composite(false, c.Rank, c.Participants));
        return c;
    }

    protected OrderedComposite<(T1, T2)> AddOrderedComposite<T1, T2>(
        Participant<T1> indexable1,
        Participant<T2> indexable2)
    {
        if (IsInvalid(out var exception, indexable1, indexable2))
            throw exception;
        OrderedComposite<(T1, T2)> c = new(_rank++, [indexable1, indexable2]);
        Composites.Add(new Composite(false, c.Rank, c.Participants));
        return c;
    }

    protected OrderedComposite<(T1, T2, T3)> AddOrderedComposite<T1, T2, T3>(
        Participant<T1> indexable1,
        Participant<T2> indexable2,
        Participant<T3> indexable3)
    {
        if (IsInvalid(out var exception, indexable1, indexable2, indexable3))
            throw exception;
        OrderedComposite<(T1, T2, T3)> c = new(_rank++, [indexable1, indexable2, indexable3]);
        return c;
    }

    protected OrderedComposite<(T1, T2, T3, T4)> AddOrderedComposite<T1, T2, T3, T4>(
        Participant<T1> indexable1,
        Participant<T2> indexable2,
        Participant<T3> indexable3,
        Participant<T4> indexable4)
    {
        if (IsInvalid(out var exception, indexable1, indexable2, indexable3, indexable4))
            throw exception;
        OrderedComposite<(T1, T2, T3, T4)> c = new(_rank++, [indexable1, indexable2, indexable3, indexable4]);
        Composites.Add(new Composite(false, c.Rank, c.Participants));
        return c;
    }

    private bool IsInvalid([MaybeNullWhen(false)] out Exception exception, params IParticipant?[] participants)
    {
        if (Locked)
        {
            exception = GenerateLockException();
            return true;
        }

        foreach (var participant in participants)
        {
            if (participant is null)
            {
                exception = new ArgumentNullException();
                return true;
            }

            if (participant.Schema != this)
            {
                exception = GenerateExternalSchemaException();
                return true;
            }
        }

        exception = null;
        return false;
    }

    private protected static Exception GenerateLockException()
    {
        return new InvalidOperationException("Once created a schema is locked and cannot be extended");
    }

    private protected static Exception GenerateExternalSchemaException()
    {
        return new ArgumentException("Attempt to use a participant generated by a distinct schema");
    }

    internal abstract ImmutableArray<Composite> GetComposites();

    internal sealed class Composite(bool unique, byte rank, ImmutableArray<IParticipant> participants)
        : IEquatable<Composite>
    {
        public ImmutableArray<IParticipant> Participants => participants;

        public byte Rank => rank;

        public bool Unique => unique;

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is Composite other && Equals(other);
        }

        public bool Equals(Composite? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;
            return Unique == other.Unique
                   && Rank == other.Rank
                   && Participants.Length == other.Participants.Length
                   && Enumerable.SequenceEqual(Participants, other.Participants);
        }

        public override int GetHashCode()
        {
            var hc = HashCode.Combine(unique, rank, participants.Length);
            return participants.Aggregate(hc, HashCode.Combine);
        }

        public override string ToString()
        {
            StringBuilder sb = new("[");
            sb.AppendJoin(" ,", Participants.Select(p => p.ItemType.Name));
            sb.Append("], Rank = ").Append(Rank);
            if (Unique) sb.Append(", Unique");
            return sb.ToString();
        }
    }
}

public abstract class Schema<TDataTuple> : Schema
    where TDataTuple : struct, ITuple, IStructuralEquatable
{
    protected sealed class Signature : ISignature
    {
        internal Signature(IEnumerable<IParticipant> participants)
        {
            Participants = [..participants.Select((p, i) => ((byte)i, p))];
        }

        public ImmutableArray<(byte, IParticipant)> Participants { get; }
    }

    public sealed override Type DataTupleType => typeof(TDataTuple);

    protected abstract Signature Sign { get; }

    internal override ISignature GetSignature() => Sign;

    protected Schema<ValueTuple<T>>.Signature Conclude<T>(Participant<T> p)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof(ValueTuple<T>) != typeof(TDataTuple))
            throw new InvalidOperationException();
        Locked = true;
        var participants = FreezeSchema(p);
        return new Schema<ValueTuple<T>>.Signature(participants);
    }

    protected Schema<(T1, T2)>.Signature Conclude<T1, T2>(Participant<T1> p1, Participant<T2> p2)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2)) != typeof(TDataTuple))
            throw new InvalidOperationException();
        Locked = true;
        var participants = FreezeSchema(p1, p2);
        return new Schema<(T1, T2)>.Signature(participants);
    }

    protected Schema<(T1, T2, T3)>.Signature Conclude<T1, T2, T3>(
        Participant<T1> p1,
        Participant<T2> p2,
        Participant<T3> p3)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2, T3)) != typeof(TDataTuple))
            throw new InvalidOperationException();
        Locked = true;
        var participants = FreezeSchema(p1, p2, p3);
        return new Schema<(T1, T2, T3)>.Signature(participants);
    }

    protected Schema<(T1, T2, T3, T4)>.Signature Conclude<T1, T2, T3, T4>(
        Participant<T1> p1,
        Participant<T2> p2,
        Participant<T3> p3,
        Participant<T4> p4)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2, T3, T4)) != typeof(TDataTuple))
            throw new InvalidOperationException();
        Locked = true;
        var participants = FreezeSchema(p1, p2, p3, p4);
        return new Schema<(T1, T2, T3, T4)>.Signature(participants);
    }

    protected Schema<(T1, T2, T3, T4, T5)>.Signature Conclude<T1, T2, T3, T4, T5>(
        Participant<T1> p1,
        Participant<T2> p2,
        Participant<T3> p3,
        Participant<T4> p4,
        Participant<T5> p5)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2, T3, T4, T5)) != typeof(TDataTuple))
            throw new InvalidOperationException();
        Locked = true;
        var participants = FreezeSchema(p1, p2, p3, p4, p5);
        return new Schema<(T1, T2, T3, T4, T5)>.Signature(participants);
    }

    protected Schema<(T1, T2, T3, T4, T5, T6)>.Signature Conclude<T1, T2, T3, T4, T5, T6>(
        Participant<T1> p1,
        Participant<T2> p2,
        Participant<T3> p3,
        Participant<T4> p4,
        Participant<T5> p5,
        Participant<T6> p6)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2, T3, T4, T5, T6)) != typeof(TDataTuple))
            throw new InvalidOperationException();
        var participants = FreezeSchema(p1, p2, p3, p4, p5, p6);
        return new Schema<(T1, T2, T3, T4, T5, T6)>.Signature(participants);
    }

    private IEnumerable<IParticipant> FreezeSchema(params IParticipant?[] signatureParticipants)
    {
        HashSet<IParticipant> declaredParticipants = [..Participants];
        foreach (var participant in signatureParticipants)
        {
            if (participant is null)
                throw new ArgumentNullException();
            if (participant.Schema != this)
                throw GenerateExternalSchemaException();
            if (!declaredParticipants.Remove(participant))
                throw new InvalidOperationException("Participants should not be used multiple times in the signature");
        }
        
        if (0 < declaredParticipants.Count)
            throw new InvalidOperationException("Some declared participants are not used in the signature");

        Locked = true;
        return signatureParticipants.Select(p => p!);
    }

    internal override ImmutableArray<Composite> GetComposites()
    {
        if (!Locked)
            throw new InvalidOperationException("Schema is not locked yet");
        return [..Composites];
    }
}