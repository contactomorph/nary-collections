using System.Collections;
using System.Runtime.CompilerServices;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

public static class DataHandling<TDataTuple, THashTuple, TIndexTuple>
    where TDataTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
{
    public static int AddOnlyData(
        ref DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        TDataTuple dataTuple,
        THashTuple hashTuple,
        ref int dataCount)
    {
        if (dataCount == dataTable.Length)
            Array.Resize(ref dataTable, dataTable.Length << 1);
        int dataIndex = dataCount;
        ++dataCount;
        
        dataTable[dataIndex] = new DataEntry<TDataTuple, THashTuple, TIndexTuple>
        {
            DataTuple = dataTuple,
            HashTuple = hashTuple,
        };
        return dataIndex;
    }
    
    public static void RemoveOnlyData(
        ref DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        int dataIndex,
        ref int dataCount)
    {
        --dataCount;
        if (dataIndex == dataCount)
        {
            dataTable[dataIndex] = default;
        }
        else
        {
            dataTable[dataIndex] = dataTable[dataCount];
            dataTable[dataCount] = default;
        }

        if (dataCount < dataTable.Length >> 2 && DataEntry.TableMinimalLength < dataTable.Length)
            Array.Resize(ref dataTable, Math.Max(dataTable.Length >> 1, DataEntry.TableMinimalLength));
    }
}