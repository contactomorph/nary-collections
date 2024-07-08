using System.Drawing;

namespace NaryCollections.Tests.Resources.Data;

public static class Colors
{
    public static readonly IReadOnlyList<Color> KnownColors =
    [
        Color.Beige,
        Color.Blue,
        Color.Cyan,
        Color.Green,
        Color.Orange,
        Color.Purple,
        Color.Red,
        Color.White,
        Color.Yellow
    ];
    
    public static readonly IReadOnlyList<Color> UnknownColors =
    [
        Color.BurlyWood,
        Color.Azure,
        Color.YellowGreen
    ];
}