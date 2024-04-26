namespace NaryCollections.Details;

public struct HashEntry
{
    public static readonly uint DriftForUnused = 0;
    
    public int ForwardIndex; // First appropriate index in the correspondence table
    public uint DriftPlusOne; // 0 if unused, if > 0 represent a drift of (DriftPlusOne - 1)
}