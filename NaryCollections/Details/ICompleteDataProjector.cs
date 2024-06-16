using System.Collections;
using System.Runtime.CompilerServices;

namespace NaryCollections.Details;

public interface ICompleteDataProjector<TDataTuple, THashTuple, TIndexTuple> :
    IDataProjector<DataEntry<TDataTuple, THashTuple, TIndexTuple>, TDataTuple>
    where TDataTuple: struct, ITuple, IStructuralEquatable
    where THashTuple: struct, ITuple, IStructuralEquatable
    where TIndexTuple: struct, ITuple, IStructuralEquatable
{
    void SetDataAt(
        DataEntry<TDataTuple, THashTuple, TIndexTuple>[] dataTable,
        int index,
        TDataTuple dataTuple,
        THashTuple hashTuple);

    THashTuple ComputeHashTuple(TDataTuple dataTuple);
}