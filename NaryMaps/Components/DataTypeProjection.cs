using System.Reflection;
using NaryMaps.Primitives;
using NaryMaps.Tools;

namespace NaryMaps.Components;

internal sealed class DataTypeProjection : DataTypeDecomposition
{
    // t: [TD1, …, TD⟨n⟩] => (t⟨i1-1⟩, …, t⟨ik-1⟩): [TD⟨i1⟩, …, TD⟨ik⟩]
    public ValueTupleMapping DataProjectionMapping { get; }
    
    // t: [uint, …, uint] => (t⟨i1-1⟩, …, t⟨ik-1⟩): [uint, …, uint]
    public ValueTupleMapping HashProjectionMapping { get; }
    
    // typeof((int, …, int)).GetField($"Item⟨r⟩")
    public FieldInfo BackIndexProjectionField { get; }
    
    // true | false
    public bool AllowsMultipleItems { get; }
    
    // int | MultiIndex
    public Type BackIndexType { get; }
    
    public DataTypeProjection(
        Type dataTupleType,
        byte backIndexRank,
        bool[] backIndexMultiplicities,
        byte[] projectionIndexes) :
        base(dataTupleType, backIndexMultiplicities)
    {
        if (projectionIndexes.Length == 0)
            throw new ArgumentException();
        if (backIndexMultiplicities.Length <= backIndexRank)
            throw new ArgumentException();
        DataProjectionMapping = ValueTupleMapping.From(DataTupleType, projectionIndexes);
        HashProjectionMapping = ValueTupleMapping.From(HashTupleType, projectionIndexes);
        BackIndexProjectionField = BackIndexTupleType[backIndexRank];
        AllowsMultipleItems = BackIndexMultiplicities[backIndexRank];
        BackIndexType = AllowsMultipleItems ? typeof(MultiIndex) : typeof(int);
    }

    public DataTypeProjection ProjectAlong(byte backIndexRank, byte[] projectionIndexes)
    {
        if (BackIndexTupleType.Count <= backIndexRank)
            throw new ArgumentException();
        return new DataTypeProjection(DataTupleType, backIndexRank, BackIndexMultiplicities, projectionIndexes);
    }
}