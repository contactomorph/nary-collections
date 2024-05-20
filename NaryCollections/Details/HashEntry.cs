using System.Diagnostics;

namespace NaryCollections.Details;

[DebuggerDisplay("{this.Debug()}")]
public struct HashEntry
{
    public static readonly uint DriftForUnused = 0;
    
    public int ForwardIndex; // First appropriate index in the correspondence table
    public uint DriftPlusOne; // 0 if unused, if > 0 represent a drift of (DriftPlusOne - 1)

    public string Debug()
    {
        return DriftPlusOne == DriftForUnused ? "\u2205" : $"{ToText(DriftPlusOne)} \u2192 {ForwardIndex}";
    }

    private string ToText(uint driftPlusOne)
    {
        if (driftPlusOne == 1) return "\u2713";
        return new string('\u25bc', (int)driftPlusOne - 1);
    }
}