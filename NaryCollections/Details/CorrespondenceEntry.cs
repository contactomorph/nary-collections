using System.Diagnostics;

namespace NaryCollections.Details;

[DebuggerDisplay("{this.Debug()}")]
public struct CorrespondenceEntry
{
    public static readonly int NoNextCorrespondence = -1;
    
    public int Previous; // index in the hash table if Status == First, previous entry in the correspondence table if Status == Subsequent
    public EntryStatus Status;
    public int DataIndex; // Index in the data table
    public int Next; // Index of next entry in the correspondence table, -1 if this is last

    public string Debug()
    {
        return Status switch
        {
            EntryStatus.Unused => "\u2205",
            EntryStatus.First => Next == NoNextCorrespondence ?
                $"{Previous} \u2190 (\u00B7 \u00B7, \u00B7 \u00B7) \u2192 {DataIndex}" :
                $"{Previous} \u2190 (\u00B7 \u00B7, \u2193 {Next}) \u2192 {DataIndex}",
            EntryStatus.Subsequent => Next == NoNextCorrespondence ?
                $"\u00B7 \u2190 (\u2191 {Previous}, \u00B7 \u00B7) \u2192 {DataIndex}" :
                $"\u00B7 \u2190 (\u2191 {Previous}, \u2193 {Next}) \u2192 {DataIndex}",
            _ => throw new InvalidDataException(),
        };
    }
}
