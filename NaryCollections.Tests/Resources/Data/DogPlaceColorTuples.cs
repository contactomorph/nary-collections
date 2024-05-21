using System.Drawing;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests.Resources.Data;

public static class DogPlaceColorTuples
{
    public static readonly (Dog Dog, string Place, Color Color)[] Data =
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
}