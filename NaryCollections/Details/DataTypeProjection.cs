using System.Reflection;

namespace NaryCollections.Details;

internal sealed class DataTypeProjection : DataTypeDecomposition
{
    // [typeof((TD1, …, TDn)).GetField($"Item{i1}"), …, typeof((TD1, …, TDn)).GetField($"Item{ik}")]
    public FieldInfo[] DataProjectionFields { get; }
    
    // [typeof(TP1), …, typeof(TPk)]
    public Type[] DataProjectionTypes { get; }
    
    // [typeof((uint, …, uint)).GetField($"Item{i1}"), …, typeof((uint, …, uint)).GetField($"Item{ik}")]
    public FieldInfo[] HashProjectionFields { get; }
    
    // typeof((int, …, int)).GetField($"Item{r}")
    public FieldInfo BackIndexProjectionField { get; }
    
    public DataTypeProjection(Type dataTupleType, byte backIndexRank, byte backIndexCount, int[] projectionIndexes) :
        base(dataTupleType, backIndexCount)
    {
        if (projectionIndexes.Length == 0)
            throw new ArgumentException();
        if (backIndexCount <= backIndexRank)
            throw new ArgumentException();
        DataProjectionFields = new FieldInfo[projectionIndexes.Length];
        HashProjectionFields = new FieldInfo[projectionIndexes.Length];
        int i = 0;
        foreach (byte index in projectionIndexes)
        {
            if (DataTypes.Length <= index)
                throw new ArgumentOutOfRangeException();
            string fieldName = $"Item{index + 1}";
            DataProjectionFields[i] = dataTupleType.GetField(fieldName) ?? throw new MissingFieldException();
            HashProjectionFields[i] = HashTupleType.GetField(fieldName) ?? throw new MissingFieldException();
            ++i;
        }
        DataProjectionTypes = DataProjectionFields.Select(f => f.FieldType).ToArray();
        BackIndexProjectionField = BackIndexTupleType.GetField($"Item{backIndexRank + 1}")
                                   ?? throw new MissingFieldException();
    }
}