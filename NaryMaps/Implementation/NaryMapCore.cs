using System.Collections;
using System.Runtime.CompilerServices;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
public abstract class NaryMapCore<TDataEntry, TComparerTuple>
    where TComparerTuple : struct, ITuple, IStructuralEquatable
{
    protected internal readonly TComparerTuple _comparerTuple;
    protected internal TDataEntry[] _dataTable;
    protected internal int _count;
    protected internal uint _version;
    
    // ReSharper disable once ConvertToPrimaryConstructor
    protected NaryMapCore(TComparerTuple comparerTuple)
    {
        _comparerTuple = comparerTuple;
        _dataTable = new TDataEntry[DataEntry.TableMinimalLength];
        _count = 0;
        _version = 0;
    }

    protected internal abstract void RemoveDataAt(int dataIndex);
}