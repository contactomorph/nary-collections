using NaryMaps.Primitives;
using NaryMaps.Tools;

namespace NaryMaps.Components;

internal class DataTypeDecomposition
{

    // typeof((TD1, …, TDn))
    public ValueTupleType DataTupleType { get; }
    
    // [typeof(IEqualityComparer<TD1>), …, typeof(IEqualityComparer<TDn>)]
    public Type[] ComparerTypes { get; }
    
    // typeof((IEqualityComparer<TD1>, …, IEqualityComparer<TDn>))
    public ValueTupleType ComparerTupleType { get; }

    // typeof((uint, …, uint))
    public ValueTupleType HashTupleType { get; }
    
    // typeof((int, …, int))
    public ValueTupleType BackIndexTupleType { get; }
    
    // typeof(DataEntry<(TD1, …, TDn), (int, …, int), (uint, …, uint)>)
    public Type DataEntryType { get; }

    // typeof(DataEntry<(TD1, …, TDn), (int, …, int), (uint, …, uint)>[])
    public Type DataTableType { get; }
    
    // [m1, …, mk]
    public bool[] BackIndexMultiplicities { get; }

    public DataTypeDecomposition(Type dataTupleType, bool[] backIndexMultiplicities)
    {
        DataTupleType = ValueTupleType.From(dataTupleType) ??
                        throw new ArgumentException("A value tuple type was expected", nameof(dataTupleType));
        HashTupleType = ValueTupleType.FromRepeatedComponent<uint>(DataTupleType.Count);

        var backIndexTypes = backIndexMultiplicities
            .Select(m => m ? typeof(MultiIndex) : typeof(int))
            .ToArray();
        
        BackIndexTupleType = ValueTupleType.FromComponents(backIndexTypes);
        
        DataEntryType = typeof(DataEntry<,,>).MakeGenericType(dataTupleType, HashTupleType, BackIndexTupleType);
        DataTableType = DataEntryType.MakeArrayType();
        
        ComparerTypes = DataTupleType
            .Select(f  => typeof(IEqualityComparer<>).MakeGenericType(f.FieldType))
            .ToArray();
        ComparerTupleType = ValueTupleType.FromComponents(ComparerTypes);
        BackIndexMultiplicities = backIndexMultiplicities;
    }
}