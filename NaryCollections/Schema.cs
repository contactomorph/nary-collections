using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace NaryCollections;

public abstract class Schema
{
    private protected bool Locked;
    
    public abstract Type ArgTupleType { get; }

    private protected Schema()
    {
        Locked = false;
    }
    
    protected Participant<T> AddParticipant<T>()
    {
        throw new NotImplementedException();
    }

    protected SearchableParticipant<T> AddSearchableParticipant<T>(
        IEqualityComparer<T>? comparer = null,
        bool unique = false)
    {
        throw new NotImplementedException();
    }
    
    protected OrderedParticipant<T> AddOrderedParticipant<T>(IComparer<T> comparer, bool unique = false)
    {
        throw new NotImplementedException();
    }
    
    protected OrderedParticipant<T> AddOrderedParticipant<T>(bool unique = false) where T : IComparable<T>
    {
        throw new NotImplementedException();
    }

    protected SearchableComposite<(T1, T2)> AddSearchableComposite<T1, T2>(
        Participant<T1> indexable1,
        Participant<T2> indexable2)
    {
        throw new NotImplementedException();
    }

    protected SearchableComposite<(T1, T2, T3)> AddSearchableInput<T1, T2, T3>(
        Participant<T1> indexable1,
        Participant<T2> indexable2,
        Participant<T3> indexable3)
    {
        throw new NotImplementedException();
    }

    protected OrderedComposite<(T1, T2)> AddOrderedComposite<T1, T2>(
        Participant<T1> indexable1,
        Participant<T2> indexable2)
    {
        throw new NotImplementedException();
    }

    protected OrderedComposite<(T1, T2, T3)> AddOrderedComposite<T1, T2, T3>(
        Participant<T1> indexable1,
        Participant<T2> indexable2,
        Participant<T3> indexable3)
    {
        throw new NotImplementedException();
    }

    private protected static Exception GenerateLockException()
    {
        return new InvalidOperationException("This");
    }

    internal abstract (Type ItemType, bool Unique)[] GetOrderedParticipants();
}

public abstract class Schema<TArgTuple> : Schema
    where TArgTuple : struct, ITuple, IStructuralEquatable
{
    protected sealed class Signature
    {
        public ImmutableArray<IParticipant> Participants { get; }
        
        internal Signature(IParticipant[] participants)
        {
            Participants = participants.ToImmutableArray();
        }
    }

    public sealed override Type ArgTupleType => typeof(TArgTuple);

    protected abstract Signature Sign { get; }

    protected Schema<ValueTuple<T>>.Signature Conclude<T>(Participant<T> p)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof(ValueTuple<T>) != typeof(TArgTuple))
            throw new InvalidOperationException();
        Locked = true;
        var signatureParticipants = CheckSignature(p);
        return new Schema<ValueTuple<T>>.Signature(signatureParticipants);
    }

    protected Schema<(T1, T2)>.Signature Conclude<T1, T2>(Participant<T1> p1, Participant<T2> p2)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2)) != typeof(TArgTuple))
            throw new InvalidOperationException();
        Locked = true;
        var signatureParticipants = CheckSignature(p1, p2);
        return new Schema<(T1, T2)>.Signature(signatureParticipants);
    }

    protected Schema<(T1, T2, T3)>.Signature Conclude<T1, T2, T3>(
        Participant<T1> p1,
        Participant<T2> p2,
        Participant<T3> p3)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2, T3)) != typeof(TArgTuple))
            throw new InvalidOperationException();
        Locked = true;
        var signatureParticipants = CheckSignature(p1, p2, p3);
        return new Schema<(T1, T2, T3)>.Signature(signatureParticipants);
    }

    protected Schema<(T1, T2, T3, T4)>.Signature Conclude<T1, T2, T3, T4>(
        Participant<T1> p1,
        Participant<T2> p2,
        Participant<T3> p3,
        Participant<T4> p4)
    {
        if (Locked)
            throw GenerateLockException();
        if (typeof((T1, T2, T3, T4)) != typeof(TArgTuple))
            throw new InvalidOperationException();
        Locked = true;
        var signatureParticipants = CheckSignature(p1, p2, p3, p4);
        return new Schema<(T1, T2, T3, T4)>.Signature(signatureParticipants);
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
        if (typeof((T1, T2, T3, T4, T5)) != typeof(TArgTuple))
            throw new InvalidOperationException();
        Locked = true;
        var signatureParticipants = CheckSignature(p1, p2, p3, p4, p5);
        return new Schema<(T1, T2, T3, T4, T5)>.Signature(signatureParticipants);
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
        if (typeof((T1, T2, T3, T4, T5, T6)) != typeof(TArgTuple))
            throw new InvalidOperationException();
        Locked = true;
        var signatureParticipants = CheckSignature(p1, p2, p3, p4, p5, p6);
        return new Schema<(T1, T2, T3, T4, T5, T6)>.Signature(signatureParticipants);
    }

    private IParticipant[] CheckSignature(params IParticipant[] signatureParticipants)
    {
        throw new NotImplementedException();
    }

    internal override (Type ItemType, bool Unique)[] GetOrderedParticipants()
    {
        return Sign.Participants.Select(p => (p.ItemType, p.Unique)).ToArray();
    }
}