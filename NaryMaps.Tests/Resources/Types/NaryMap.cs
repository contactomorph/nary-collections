using System.Drawing;
using NaryMaps.Implementation;
using NaryMaps.Primitives;
#pragma warning disable CS9113 // Parameter is unread.

namespace NaryMaps.Tests.Resources.Types;

using DataTuple = (Dog Dog, string Place, Color Color);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

public struct CompositeHandlerA(string message) :
    IHashTableProvider,
    IDataEquator<DogPlaceColorEntry, ComparerTuple, (Color, Dog)>
{
    private HashEntry[] _hashTable = new HashEntry[30];
    public HashEntry[] GetHashTable() => _hashTable;
    public int GetHashEntryCount() => -1;
    public bool AreDataEqualAt(
        DogPlaceColorEntry[] dataTable,
        ComparerTuple comparerTuple,
        int index,
        (Color, Dog) item,
        uint hashCode)
    {
        return false;
    }
}

public struct CompositeHandlerB(string message) :
    IHashTableProvider,
    IDataEquator<DogPlaceColorEntry, ComparerTuple, string>
{
    private HashEntry[] _hashTable = new HashEntry[100];
    private int _count = 42;
    public HashEntry[] GetHashTable() => _hashTable;
    public int GetHashEntryCount() => _count;
    public bool AreDataEqualAt(
        DogPlaceColorEntry[] dataTable,
        ComparerTuple comparerTuple,
        int index,
        string item,
        uint hashCode)
    {
        return false;
    }
}

public sealed class NaryMapCore(ComparerTuple comparerTuple)
    : NaryMapCore<DogPlaceColorEntry, ComparerTuple>(comparerTuple)
{
    public CompositeHandlerA _compositeHandlerA = new ("Primo");
    public CompositeHandlerB _compositeHandlerB = new ("Deuzio");
}

public abstract class ColorDogSelection(NaryMapCore<DogPlaceColorEntry, ComparerTuple> map) :
    SelectionBase<DataTuple, DogPlaceColorEntry, ComparerTuple, CompositeHandlerA, (Color, Dog)>(map)
{
    public override int GetKeyCount() => int.MaxValue;
    public override DataTuple? GetFirstDataTupleFor((Color, Dog) item) => null;
    public override IEnumerable<DataTuple>? GetDataTuplesFor((Color, Dog) item) => null;
    public override IEnumerable<(Color, Dog)> GetItemEnumerable() => [];
    public override IEnumerable<KeyValuePair<(Color, Dog), IEnumerable<DataTuple>>> GetItemAndDataTuplesEnumerable() => [];
}

public abstract class PlaceSelection(NaryMapCore<DogPlaceColorEntry, ComparerTuple> map) :
    SelectionBase<DataTuple, DogPlaceColorEntry, ComparerTuple, CompositeHandlerB, string>(map)
{
    public override int GetKeyCount() => int.MaxValue;
    public override DataTuple? GetFirstDataTupleFor(string item) => null;
    public override IEnumerable<DataTuple>? GetDataTuplesFor(string item) => null;
    public override IEnumerable<string> GetItemEnumerable() => [];
    public override IEnumerable<KeyValuePair<string, IEnumerable<DataTuple>>> GetItemAndDataTuplesEnumerable() => [];
}