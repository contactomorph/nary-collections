namespace NaryCollections.Primitives;

public struct MultiIndex
{
    public static readonly int NoNext = -1;
    
    // index in the hash table if !Subsequent
    // previous entry in the data table if Subsequent
    public int Previous;
    public bool IsSubsequent;
    public int Next; // Index of next entry in the data table, -1 if this is last

    public override string ToString()
    {
        if (IsSubsequent)
        {
            return Next == NoNext
                ? $"\u00B7 \u2190 (\u2191 {Previous}, \u00B7 \u00B7)"
                : $"\u00B7 \u2190 (\u2191 {Previous}, \u2193 {Next})";
        }
        else
        {
            return Next == NoNext
                ? $"{Previous} \u2190 (\u00B7 \u00B7, \u00B7 \u00B7) \u2192"
                : $"{Previous} \u2190 (\u00B7 \u00B7, \u2193 {Next}) \u2192";
        }
    }
}
