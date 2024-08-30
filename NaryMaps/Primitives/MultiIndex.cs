namespace NaryMaps.Primitives;

public struct MultiIndex
{
    public const int NoNext = -1;
    
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
                ? $"\u2329 \u2191{Previous} \u232a"
                : $"\u2329 \u2191{Previous} \u00b7 \u2193{Next} \u232a";
        }
        else
        {
            return Next == NoNext
                ? $"\u2329 \u2190{Previous} \u232a"
                : $"\u2329 \u2190{Previous} \u00b7 \u2193{Next} \u232a";
        }
    }
}
