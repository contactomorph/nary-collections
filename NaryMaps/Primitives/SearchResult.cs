namespace NaryMaps.Primitives;

public enum SearchCase
{
    EmptyEntryFound = 0,
    SearchStopped = 1,
    ItemFound = 2,
}

public record struct SearchResult(SearchCase Case, uint ReducedHashCode, uint DriftPlusOne, int ForwardIndex)
{
    public static SearchResult CreateForEmptyEntry(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.EmptyEntryFound, reducedHash, driftPlusOne, -1);
    }
    
    public static SearchResult CreateWhenSearchStopped(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.SearchStopped, reducedHash, driftPlusOne, -1);
    }
    
    public static SearchResult CreateForItemFound(uint reducedHash, uint driftPlusOne, int forwardIndex)
    {
        return new SearchResult(SearchCase.ItemFound, reducedHash, driftPlusOne, forwardIndex);
    }
}

