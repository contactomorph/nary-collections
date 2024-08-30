using System.Drawing;

namespace NaryMaps.Tests.Resources.Types;

public sealed class DogPlaceColor : Schema<(Dog Dog, string Place, Color Color)>
{
    public DogPlaceColor()
    {
        Dog = AddSearchableParticipant<Dog>();
        Color = AddSearchableParticipant<Color>();
        Name = AddOrderedParticipant<string>(unique: true);
        DogColor = AddSearchableComposite(Dog, Color);
        NameColor = AddOrderedComposite(Name, Color);
        Sign = Conclude(Dog, Name, Color);
    }

    public SearchableParticipant<Dog> Dog { get; }
    public SearchableParticipant<Color> Color { get; }

    public OrderedParticipant<string> Name { get; }

    public SearchableComposite<(Dog, Color)> DogColor { get; }

    public OrderedComposite<(string, Color)> NameColor { get; }
    protected override Signature Sign { get; }
}