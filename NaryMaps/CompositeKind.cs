namespace NaryMaps;

public static class CompositeKind
{
    public class Basic { internal Basic() {} }
    public interface ISearchable { }
    public interface IUnique { }
    public sealed class Searchable : Basic, ISearchable { private Searchable() {} }
    public sealed class UniqueSearchable : Basic, ISearchable, IUnique { private UniqueSearchable() {} }
}