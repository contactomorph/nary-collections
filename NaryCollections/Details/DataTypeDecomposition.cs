using System.Reflection;
using NaryCollections.Tools;

namespace NaryCollections.Details;

internal class DataTypeDecomposition
{
    // typeof((TD1, …, TDn))
    public ValueTupleType DataTupleType { get; }
    
    // [typeof(IEqualityComparer<TD1>), …, typeof(IEqualityComparer<TDn>)]
    public Type[] ComparerTypes { get; }

    // typeof((uint, …, uint))
    public ValueTupleType HashTupleType { get; }
    
    // typeof((int, …, int))
    public ValueTupleType BackIndexTupleType { get; }
    
    // typeof(DataEntry<(TD1, …, TDn), (int, …, int), (uint, …, uint)>)
    public Type DataEntryType { get; }

    // typeof(DataEntry<(TD1, …, TDn), (int, …, int), (uint, …, uint)>[])
    public Type DataTableType { get; }

    public DataTypeDecomposition(Type dataTupleType, byte backIndexCount)
    {
        DataTupleType = ValueTupleType.From(dataTupleType) ??
                        throw new ArgumentException("A value tuple type was expected", nameof(dataTupleType));
        HashTupleType = ValueTupleType.FromRepeatedComponent<uint>(DataTupleType.Count);
        BackIndexTupleType = ValueTupleType.FromRepeatedComponent<int>(backIndexCount);
        
        DataEntryType = typeof(DataEntry<,,>).MakeGenericType(dataTupleType, HashTupleType, BackIndexTupleType);
        DataTableType = DataEntryType.MakeArrayType();
        
        ComparerTypes = DataTupleType
            .Select(f  => typeof(IEqualityComparer<>).MakeGenericType(f.FieldType))
            .ToArray();
    }
}