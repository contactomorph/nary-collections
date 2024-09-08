using System.Drawing;
using NaryMaps.Primitives;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests.Resources.Data;

public static class DogPlaceColorTuples
{
    public static readonly IReadOnlyList<(Dog Dog, string Place, Color Color)> Data =
    [
        (Dogs.KnownDogs[0], "Lyon", Color.Beige),
        (Dogs.KnownDogs[1], "Lyon", Color.CadetBlue),
        (Dogs.KnownDogs[2], "Paris", Color.Beige),
        (Dogs.KnownDogs[2], "Lyon", Color.CadetBlue),
        (Dogs.KnownDogs[1], "Lyon", Color.Orange),
        (Dogs.KnownDogs[1], "Bordeaux", Color.CadetBlue),
        (Dogs.KnownDogs[0], "Bordeaux", Color.Beige),
        (Dogs.KnownDogs[0], "Bordeaux", Color.CadetBlue),
    ];
    
    public static readonly IReadOnlyList<(Dog Dog, string Place, Color Color)> DataWithUniquePlace =
    [
        (Dogs.KnownDogs[0], "Київ", Color.Beige),
        (Dogs.KnownDogs[1], "Харків", Color.CadetBlue),
        (Dogs.KnownDogs[2], "Одеса", Color.Beige),
        (Dogs.KnownDogs[2], "Дніпро", Color.Plum),
        (Dogs.KnownDogs[1], "Донецьк", Color.Orange),
        (Dogs.KnownDogs[1], "Запоріжжя", Color.CadetBlue),
        (Dogs.KnownDogs[0], "Львів", Color.Beige),
        (Dogs.KnownDogs[0], "Кривий Ріг", Color.CadetBlue),
        (Dogs.UnknownDogs[0], "Миколаїв", Color.Plum),
    ];
    
    public static readonly IReadOnlyList<(Dog Dog, string Place, Color Color)> DataWithUniqueDogs =
    [
        (Dogs.KnownDogs[0], "Lyon", Color.Beige),
        (Dogs.KnownDogs[1], "Lyon", Color.CadetBlue),
        (Dogs.KnownDogs[2], "Paris", Color.Beige),
    ];

    // for Dogs.DogsWithHashCode
    public static readonly IReadOnlyList<HashEntry> ExpectedHashTableSource = 
    [
        /*  0 */ default,
        /*  1 */ new HashEntry { DriftPlusOne = HashEntry.Optimal, ForwardIndex = 0 }, // ok
        /*  2 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 1 }, // should be in 1
        /*  3 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 2 }, // should be in 2
        /*  4 */ default,
        /*  5 */ new HashEntry { DriftPlusOne = HashEntry.Optimal, ForwardIndex = 3 }, // ok
        /*  6 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 4 }, // should be in 5
        /*  7 */ new HashEntry { DriftPlusOne = 3, ForwardIndex = 5 }, // should be in 5
        /*  8 */ new HashEntry { DriftPlusOne = 3, ForwardIndex = 6 }, // should be in 6
        /*  9 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 7 }, // should be in 8
        /* 10 */ new HashEntry { DriftPlusOne = HashEntry.Optimal, ForwardIndex = 8 }, // ok
        /* 11 */ default,
        /* 12 */ default,
    ];
    
    // for Dogs.DogsWithHashCode
    public static readonly IReadOnlyList<HashEntry> ExpectedHashTableSource2 = 
    [
        /*  0 */ default,
        /*  1 */ new HashEntry { DriftPlusOne = HashEntry.Optimal, ForwardIndex = 2 }, // ok
        /*  2 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 3 }, // should be in 1
        /*  3 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 5 }, // should be in 2
        /*  4 */ default,
        /*  5 */ new HashEntry { DriftPlusOne = HashEntry.Optimal, ForwardIndex = 7 }, // ok
        /*  6 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 9 }, // should be in 5
        /*  7 */ new HashEntry { DriftPlusOne = 3, ForwardIndex = 10 }, // should be in 5
        /*  8 */ new HashEntry { DriftPlusOne = 3, ForwardIndex = 13 }, // should be in 6
        /*  9 */ new HashEntry { DriftPlusOne = 2, ForwardIndex = 14 }, // should be in 8
        /* 10 */ new HashEntry { DriftPlusOne = HashEntry.Optimal, ForwardIndex = 16 }, // ok
        /* 11 */ default,
        /* 12 */ default,
    ];
}