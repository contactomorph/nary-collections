using System.Collections;
using System.Drawing;
using NaryMaps.Implementation;
using NaryMaps.Primitives;

namespace NaryMaps.Tests.Resources.Types;

using DataTuple = (Dog Dog, string Place, Color Color);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

public struct CompositeHandlerA(string Message) : IHashTableProvider
{
    private HashEntry[] _hashTable = new HashEntry[30];
    public HashEntry[] GetHashTable() => _hashTable;
    public int GetHashEntryCount() => -1;
}

public struct CompositeHandlerB(string Message) : IHashTableProvider
{
    private HashEntry[] _hashTable = new HashEntry[100];
    private int _count = 42;
    public HashEntry[] GetHashTable() => _hashTable;
    public int GetHashEntryCount() => _count;
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
    protected override IEnumerator<(Color, Dog)> GetKeyEnumerator() => Enumerable.Empty<(Color, Dog)>().GetEnumerator();
    protected override IEnumerator GetPairEnumerator() => Enumerable.Empty<ValueTuple>().GetEnumerator();
    protected override int GetKeyCount() => int.MaxValue;
    protected override bool ContainsAsKey((Color, Dog) item) => false;
}

public abstract class PlaceSelection(NaryMapCore<DogPlaceColorEntry, ComparerTuple> map) :
    SelectionBase<DataTuple, DogPlaceColorEntry, ComparerTuple, CompositeHandlerB, string>(map)
{
    protected override IEnumerator<string> GetKeyEnumerator() => Enumerable.Empty<string>().GetEnumerator();
    protected override IEnumerator GetPairEnumerator() => Enumerable.Empty<ValueTuple>().GetEnumerator();
    protected override int GetKeyCount() => int.MaxValue;
    protected override bool ContainsAsKey(string item) => false;
}