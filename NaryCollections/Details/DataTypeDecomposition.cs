using System.Reflection;

namespace NaryCollections.Details;

internal class DataTypeDecomposition
{
    // typeof((TD1, …, TDn))
    public Type DataTupleType { get; }

    // [typeof(TD1), …, typeof(TDn)]
    public Type[] DataTypes { get; }
    
    // [typeof(IEqualityComparer<TD1>), …, typeof(IEqualityComparer<TDn>)]
    public Type[] ComparerTypes { get; }
    
    // [typeof(IEqualityComparer<TD1>).GetMethod("Equals"), …, typeof(IEqualityComparer<TDn>).GetMethod("Equals")]
    public MethodInfo[] ComparerMethods { get; }

    // typeof((uint, …, uint))
    public Type HashTupleType { get; }
    
    // typeof((int, …, int))
    public Type BackIndexTupleType { get; }
    
    // typeof(DataEntry<(TD1, …, TDn), (int, …, int), (uint, …, uint)>)
    public Type DataEntryType { get; }

    // typeof(DataEntry<(TD1, …, TDn), (int, …, int), (uint, …, uint)>[])
    public Type DataTableType { get; }

    public DataTypeDecomposition(Type dataTupleType, byte backIndexCount)
    {
        DataTupleType = dataTupleType;
        DataTypes = TupleHandling.GetTupleTypeComposition(dataTupleType);
        ComparerMethods = DataTypes.Select(GetEqualsMethod).ToArray();
        ComparerTypes = ComparerMethods.Select(m => m.DeclaringType!).ToArray();

        HashTupleType = TupleHandling.GetRepeatedTupleType<uint>(DataTypes.Length);
        BackIndexTupleType = TupleHandling.GetRepeatedTupleType<int>(backIndexCount);
        
        DataEntryType = typeof(DataEntry<,,>).MakeGenericType(dataTupleType, HashTupleType, BackIndexTupleType);
        DataTableType = DataEntryType.MakeArrayType();
    }

    private static MethodInfo GetEqualsMethod(Type t)
    {
        return typeof(IEqualityComparer<>)
            .MakeGenericType(t)
            .GetMethod(nameof(IEqualityComparer<object>.Equals), [t, t])!;
    }
}