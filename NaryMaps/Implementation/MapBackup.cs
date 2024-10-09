using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

internal sealed class MapBackup<TDataEntry, TComparerTuple> where TComparerTuple : struct, ITuple, IStructuralEquatable
{
    private readonly (FieldInfo f, IHashTableProvider handler)[] _handlers;
    private readonly int _count;
    private readonly TDataEntry[] _dataTable;
    private bool _used;

    public MapBackup(NaryMapCore<TDataEntry, TComparerTuple> map)
    {
        _handlers = map
            .GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.Name.StartsWith("_compositeHandler"))
            .Select(f => CopyHandler(map, f))
            .ToArray();
        _count = map._count;
        _dataTable = map._dataTable.ToArray();
        _used = false;
    }

    private (FieldInfo f, IHashTableProvider handler) CopyHandler(
        NaryMapCore<TDataEntry, TComparerTuple> map,
        FieldInfo f)
    {
        var handler = (IHashTableProvider)f.GetValue(map)!;
        var htField = handler
            .GetType()
            .GetField("_hashTable", BindingFlags.NonPublic | BindingFlags.Instance)!;
                
        var ht = (HashEntry[])htField.GetValue(handler)!;
        htField.SetValue(handler, ht.ToArray());
                    
        return (f, handler);
    }

    public void Reset(NaryMapCore<TDataEntry, TComparerTuple> map)
    {
        if (_used)
            throw new InvalidOperationException();
        _used = true;
        map._count = _count;
        map._dataTable = _dataTable;
        foreach (var (f, handler) in _handlers) f.SetValue(map, handler);
    }
}