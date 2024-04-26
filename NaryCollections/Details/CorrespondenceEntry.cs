namespace NaryCollections.Details;

public struct CorrespondenceEntry
{
    public static readonly int NoNextCorrespondence = -1;
    
    public int BackIndex; // index in the hash table if Status == First, previous entry in the correspondence table if Status == Subsequent
    public EntryStatus Status;
    public int DataIndex; // Index in the data table
    public int Next; // Index of next entry in the correspondence table, -1 if this is last
}
