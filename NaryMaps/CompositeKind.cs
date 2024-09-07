namespace NaryMaps;

public static class CompositeKind
{
    public class Basic { internal Basic() {} }
    public interface ISearchable { }
    public interface IOrdered : ISearchable { }
    public interface IUnique { }
    public sealed class Searchable : Basic, ISearchable { private Searchable() {} }
    public sealed class UniqueSearchable : Basic, ISearchable, IUnique { private UniqueSearchable() {} }
    public sealed class Ordered : Basic, IOrdered { private Ordered() {} }
    public sealed class UniqueOrdered : Basic, IOrdered, IUnique { private UniqueOrdered() {} }
}