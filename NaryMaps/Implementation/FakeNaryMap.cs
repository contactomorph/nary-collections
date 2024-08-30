using NaryMaps.Primitives;

namespace NaryMaps.Implementation;

using FakeDataEntry = DataEntry<ValueTuple, ValueTuple, ValueTuple>;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class FakeNaryMap :
    NaryMapBase<ValueTuple, ValueTuple, ValueTuple, ValueTuple, FakeNaryMap.FakeCompositeHandler, FakeNaryMap.FakeSchema>
{
    public const string ComparerTupleFieldName = nameof(_comparerTuple);
    public const string DataTableFieldName = nameof(_dataTable);
    public const string ComputeHashTupleMethodName = nameof(ComputeHashTuple);
    public const string FindInOtherCompositesMethodName = nameof(FindInOtherComposites);
    public const string AddToOtherCompositesMethodName = nameof(AddToOtherComposites);
    public const string RemoveFromOtherCompositesMethodName = nameof(RemoveFromOtherComposites);
    
    private FakeNaryMap() : base(new(), new (), new ()) { }

    protected override ValueTuple ComputeHashTuple(ValueTuple dataTuple) => throw new NotImplementedException();

    protected override bool FindInOtherComposites(
        ValueTuple dataTuple,
        ValueTuple hashTuple,
        out SearchResult[] otherResults)
    {
        throw new NotImplementedException();
    }

    protected override void AddToOtherComposites(SearchResult[] otherResults, int candidateDataIndex)
    {
        throw new NotImplementedException();
    }

    protected override void RemoveFromOtherComposites(int removedDataIndex)
    {
        throw new NotImplementedException();
    }
    
    
    public readonly struct FakeCompositeHandler :
        ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, ValueTuple>
    {
        public SearchResult Find(
            FakeDataEntry[] dataTable,
            ValueTuple comparerTuple,
            uint candidateHashCode,
            ValueTuple candidateItem)
        {
            throw new NotImplementedException();
        }

        public void Add(
            FakeDataEntry[] dataTable,
            SearchResult lastSearchResult,
            int candidateDataIndex,
            int newDataCount)
        {
            throw new NotImplementedException();
        }

        public void Remove(FakeDataEntry[] dataTable, int removedDataIndex, int newDataCount)
        {
            throw new NotImplementedException();
        }

        public void Clear() => throw new NotImplementedException();
    }

    public sealed class FakeSchema : Schema<ValueTuple>
    {
        protected override Signature Sign => throw new NotImplementedException();
    }
}