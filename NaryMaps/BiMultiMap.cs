namespace NaryMaps;

public static class BiMultiMap
{
    public static INaryMap<Schema<TDirect, TInverse>> New<TDirect, TInverse>()
    {
        return NaryMap.New<Schema<TDirect, TInverse>>();
    }
    
    public sealed class Schema<TDirect, TInverse> : Schema<(TDirect, TInverse)>
    {
        public SearchableParticipant<TInverse> Inverse { get;  }

        public SearchableParticipant<TDirect> Direct { get; }
    
        public Schema()
        {
            Direct = DeclareSearchableParticipant<TDirect>();
            Inverse = DeclareSearchableParticipant<TInverse>();
            Sign = Conclude(Direct, Inverse);
        }

        protected override Signature Sign { get; }
    }
}