namespace NaryCollections.Primitives;

internal static class HashCodeReduction
{
    public static uint ComputeReducedHashCode(uint candidateHashCode, int hashTableLength)
    {
        return candidateHashCode % (uint)hashTableLength;
    }

    public static void MoveReducedHashCode(ref uint reducedHashCode, int hashTableLength)
    {
        reducedHashCode = (reducedHashCode + 1) % (uint)hashTableLength;
    }
}