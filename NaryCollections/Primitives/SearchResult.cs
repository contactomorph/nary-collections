namespace NaryCollections.Primitives;

public enum SearchCase
{
    EmptyEntryFound = 0,
    SearchStopped = 1,
    ItemFound = 2,
}

public record struct SearchResult(SearchCase Case, int HashIndex, uint DriftPlusOne)
{
    public static SearchResult CreateForEmptyEntry(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.EmptyEntryFound, (int)reducedHash, driftPlusOne);
    }
    
    public static SearchResult CreateWhenSearchStopped(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.SearchStopped, (int)reducedHash, driftPlusOne);
    }
    
    public static SearchResult CreateForItemFound(uint reducedHash, uint driftPlusOne)
    {
        return new SearchResult(SearchCase.ItemFound, (int)reducedHash, driftPlusOne);
    }
}

