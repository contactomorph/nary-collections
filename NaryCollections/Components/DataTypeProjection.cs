using System.Reflection;
using NaryCollections.Tools;

namespace NaryCollections.Components;

internal sealed class DataTypeProjection : DataTypeDecomposition
{
    // t: [TD1, …, TD⟨n⟩] => (t⟨i1-1⟩, …, t⟨ik-1⟩): [TD⟨i1⟩, …, TD⟨ik⟩]
    public ValueTupleMapping DataProjectionMapping { get; }
    
    // t: [uint, …, uint] => (t⟨i1-1⟩, …, t⟨ik-1⟩): [uint, …, uint]
    public ValueTupleMapping HashProjectionMapping { get; }
    
    // typeof((int, …, int)).GetField($"Item⟨r⟩")
    public FieldInfo BackIndexProjectionField { get; }
    
    public DataTypeProjection(Type dataTupleType, byte backIndexRank, byte backIndexCount, byte[] projectionIndexes) :
        base(dataTupleType, backIndexCount)
    {
        if (projectionIndexes.Length == 0)
            throw new ArgumentException();
        if (backIndexCount <= backIndexRank)
            throw new ArgumentException();
        DataProjectionMapping = ValueTupleMapping.From(DataTupleType, projectionIndexes);
        HashProjectionMapping = ValueTupleMapping.From(HashTupleType, projectionIndexes);
        BackIndexProjectionField = BackIndexTupleType[backIndexRank];
    }

    public DataTypeProjection ProjectAlong(byte backIndexRank, byte[] projectionIndexes)
    {
        if (BackIndexTupleType.Count <= backIndexRank)
            throw new ArgumentException();
        return new DataTypeProjection(DataTupleType, backIndexRank, (byte)BackIndexTupleType.Count, projectionIndexes);
    }
}