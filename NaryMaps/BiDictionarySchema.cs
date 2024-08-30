using System.Diagnostics.Contracts;

namespace NaryMaps;

public sealed class BiDictionarySchema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
{
    public SearchableParticipant<TInverse> Inverse { get;  }

    public SearchableParticipant<TDirect> Direct { get; }
    
    public BiDictionarySchema()
    {
        Direct = AddSearchableParticipant<TDirect>(unique: true);
        Inverse = AddSearchableParticipant<TInverse>(unique: true);
        Sign = Conclude(Direct, Inverse);
    }

    protected override Signature Sign { get; }
}

public sealed class BiMultiDictionarySchema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
{
    public SearchableParticipant<TInverse> Inverse { get;  }

    public SearchableParticipant<TDirect> Direct { get; }
    
    public BiMultiDictionarySchema()
    {
        Direct = AddSearchableParticipant<TDirect>(unique: false);
        Inverse = AddSearchableParticipant<TInverse>(unique: false);
        Sign = Conclude(Direct, Inverse);
    }

    protected override Signature Sign { get; }
}