using NaryMaps.Primitives;

namespace NaryMaps.Fakes;

public struct FakeHashTableProvider : IHashTableProvider
{
    public HashEntry[] GetHashTable() => throw new NotImplementedException();

    public int GetHashEntryCount() => throw new NotImplementedException();
}