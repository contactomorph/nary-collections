using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryMaps.Primitives;

public static class DataEntry
{
    public const int TableMinimalLength = 8;
}

public struct DataEntry<TDataTuple, THashTuple, TIndexTuple>
    where TDataTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
{
    public TDataTuple DataTuple;
    public THashTuple HashTuple;
    public TIndexTuple BackIndexesTuple;

    public override string ToString()
    {
        return $"{DataTuple}, {HashTuple}, {BackIndexesTuple}";
    }
}