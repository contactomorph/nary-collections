using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.Data;

public static class Dogs
{
    public static readonly IReadOnlyList<Dog> KnownDogs =
    [
        new("Rex", "Anne Besnard"),
        new("Fifi", "Louis Durand"),
        new("Platon", "Jean Dupuis"),
    ];

    public static readonly IReadOnlyList<Dog> UnknownDogs =
    [
        new("Fifi", "Louis Dupont"),
        new("Rex", "Jean Dupuis"),
    ];
}