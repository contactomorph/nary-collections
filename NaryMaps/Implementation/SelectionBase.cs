using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

public abstract class SelectionBase<TDataTuple, TDataEntry, TComparerTuple, THandler, T>
    where TDataTuple : struct, ITuple, IStructuralEquatable
    where TDataEntry : struct
    where TComparerTuple : struct, ITuple, IStructuralEquatable
    where THandler : struct, IHashTableProvider
{
    // ReSharper disable once InconsistentNaming
    protected readonly NaryMapCore<TDataEntry, TComparerTuple> _map;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected SelectionBase(NaryMapCore<TDataEntry, TComparerTuple> map) => _map = map;
   
    protected internal abstract THandler GetHandler();
    protected internal abstract T GetItem(TDataEntry dataEntry);
    protected internal abstract TDataTuple GetDataTuple(TDataEntry dataEntry);
    protected internal abstract uint ComputeHashCode(TComparerTuple comparerTuple, T item);
}