using System.Reflection;
using NaryCollections.Tools;

namespace NaryCollections.Details;

internal sealed class DataTypeProjection : DataTypeDecomposition
{
    // t: [TD1, …, TD⟨n⟩] => (t.Item⟨i1⟩, …, t.Item⟨ik⟩): [TD⟨i1⟩, …, TD⟨ik⟩]
    public ValueTupleMapping DataProjectionMapping { get; }
    
    // t: [uint, …, uint] => (t.Item⟨i1⟩, …, t.Item⟨ik⟩): [uint, …, uint]
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
}