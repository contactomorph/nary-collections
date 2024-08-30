using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests.Resources.Data;

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
    
    public static readonly IReadOnlyList<(Dog Dog, uint HashCode)> KnownDogsWithHashCode =
    [
        (KnownDogs[0], 1),
        (KnownDogs[1], 1),
        (KnownDogs[2], 2),
        (new("Quichote","Sylvain Ducreux"), 5),
        (new("Lula","Lucie Beauregard"), 5),
        (new("Goldorak", "Louis Durand"), 5),
        (new("Terminator", "Geoffroy Monchamp"), 6),
        (new("Chouchou", "Amanda Humbert"), 8),
        (new("Alpha", "Bernadette Granger"), 10),
    ];
    
    public static readonly IReadOnlyList<(Dog Dog, uint HashCode)> NewDogsWithHashCode =
    [
        (UnknownDogs[0], 4),
        (UnknownDogs[1], 3),
        (new ("Minou", "Gaspard Mounier"), 1),
        (new ("Philibert", "Gaspard Mounier"), 5),
    ];


    public static readonly IReadOnlyList<Dog> AllDogs = KnownDogsWithHashCode
        .Concat(NewDogsWithHashCode)
        .Select(p => p.Dog)
        .ToArray();
}