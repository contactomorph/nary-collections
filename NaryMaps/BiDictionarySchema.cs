namespace NaryMaps;

public sealed class BiDictionarySchema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
{
    public UniqueSearchableParticipant<TInverse> Inverse { get;  }

    public UniqueSearchableParticipant<TDirect> Direct { get; }
    
    public BiDictionarySchema()
    {
        Direct = DeclareUniqueSearchableParticipant<TDirect>();
        Inverse = DeclareUniqueSearchableParticipant<TInverse>();
        Sign = Conclude(Direct, Inverse);
    }

    protected override Signature Sign { get; }
}

public sealed class BiMultiDictionarySchema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
{
    public UniqueSearchableParticipant<TInverse> Inverse { get;  }

    public UniqueSearchableParticipant<TDirect> Direct { get; }
    
    public BiMultiDictionarySchema()
    {
        Direct = DeclareUniqueSearchableParticipant<TDirect>();
        Inverse = DeclareUniqueSearchableParticipant<TInverse>();
        Sign = Conclude(Direct, Inverse);
    }

    protected override Signature Sign { get; }
}