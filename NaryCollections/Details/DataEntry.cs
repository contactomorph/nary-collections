using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections.Details;

public static class DataEntry
{
    public const int TableMinimalLength = 8;
}

public struct DataEntry<TArgTuple, THashTuple, TIndexTuple>
    where TArgTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
{
    public TArgTuple DataTuple;
    public THashTuple HashTuple;
    public TIndexTuple BackIndexesTuple;

    public override string ToString()
    {
        return $"{DataTuple}, {HashTuple}, {BackIndexesTuple}";
    }
}