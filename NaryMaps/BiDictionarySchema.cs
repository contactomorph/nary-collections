namespace NaryMaps;

public sealed class BiDictionarySchema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
{
    public UniqueSearchableParticipant<TInverse> Inverse { get;  }

    public UniqueSearchableParticipant<TDirect> Direct { get; }
    
    public BiDictionarySchema(
        IEqualityComparer<TDirect>? directComparer = null,
        IEqualityComparer<TInverse>? inverseComparer = null)
    {
        Direct = DeclareUniqueSearchableParticipant(directComparer);
        Inverse = DeclareUniqueSearchableParticipant(inverseComparer);
        Sign = Conclude(Direct, Inverse);
    }

    protected override Signature Sign { get; }
}

public sealed class BiMultiDictionarySchema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
{
    public SearchableParticipant<TInverse> Inverse { get;  }

    public SearchableParticipant<TDirect> Direct { get; }
    
    public BiMultiDictionarySchema(
        IEqualityComparer<TDirect>? directComparer = null,
        IEqualityComparer<TInverse>? inverseComparer = null)
    {
        Direct = DeclareSearchableParticipant(directComparer);
        Inverse = DeclareSearchableParticipant(inverseComparer);
        Sign = Conclude(Direct, Inverse);
    }

    protected override Signature Sign { get; }
}