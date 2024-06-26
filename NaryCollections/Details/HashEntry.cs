namespace NaryCollections.Details;

public struct HashEntry
{
    public const int TableMinimalLength = 7;
    
    public static readonly uint DriftForUnused = 0;
    public static readonly uint Optimal = 1;
    
    public int ForwardIndex; // First appropriate index in the correspondence table
    public uint DriftPlusOne; // 0 if unused, if > 0 represent a drift of (DriftPlusOne - 1)

    public override string ToString()
    {
        return DriftPlusOne == DriftForUnused ? "\u2205" : $"{ToText(DriftPlusOne)} \u2192 {ForwardIndex}";
    }

    private string ToText(uint driftPlusOne)
    {
        if (driftPlusOne == Optimal) return "\u2713";
        return new string('\u25bc', (int)driftPlusOne - 1);
    }

    public static bool IsFullEnough(int capacity, int count)
    {
        return capacity * 0.75 < count;
    }

    public static bool IsSparseEnough(int capacity, int count)
    {
        return TableMinimalLength < capacity && count < capacity * 0.15;
    }
    
    public static int IncreaseCapacity(int capacity) => capacity * 2 + 1;
    
    public static int DecreaseCapacity(int capacity) => capacity / 2;
}