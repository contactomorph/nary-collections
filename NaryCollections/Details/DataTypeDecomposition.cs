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
    public MethodInfo[] ComparerEqualsMethods { get; }

    // [typeof(IEqualityComparer<TD1>).GetMethod("GetHashCode"), …, typeof(IEqualityComparer<TDn>).GetMethod("GetHashCode")]
    public MethodInfo[] ComparerGetHashCodeMethods { get; }

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

        HashTupleType = TupleHandling.GetRepeatedTupleType<uint>(DataTypes.Length);
        BackIndexTupleType = TupleHandling.GetRepeatedTupleType<int>(backIndexCount);
        
        DataEntryType = typeof(DataEntry<,,>).MakeGenericType(dataTupleType, HashTupleType, BackIndexTupleType);
        DataTableType = DataEntryType.MakeArrayType();
        
        ComparerTypes = new Type[DataTypes.Length];
        ComparerEqualsMethods = new MethodInfo[DataTypes.Length];
        ComparerGetHashCodeMethods = new MethodInfo[DataTypes.Length];
        CreateComparerRelatedValues();
    }

    private void CreateComparerRelatedValues()
    {
        int i = 0;
        foreach (var dataType in DataTypes)
        {
            var comparerType = typeof(IEqualityComparer<>).MakeGenericType(dataType);
            ComparerTypes[i] = comparerType;
        
            ComparerEqualsMethods[i] =
                comparerType.GetMethod(nameof(IEqualityComparer<object>.Equals), [dataType, dataType]) ??
                throw new InvalidProgramException();
            ComparerGetHashCodeMethods[i] =
                comparerType.GetMethod(nameof(IEqualityComparer<object>.GetHashCode), [dataType]) ??
                throw new InvalidProgramException();
            
            ++i;
        }
    }
}