namespace NaryMaps.Primitives;

public interface IHashTableProvider
{
    public HashEntry[] GetHashTable();
    public int GetHashEntryCount();
}