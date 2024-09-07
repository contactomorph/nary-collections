using System.Drawing;

namespace NaryMaps.Tests.Resources.Types;

public sealed class DogPlaceColor : Schema<(Dog Dog, string Place, Color Color)>
{
    public DogPlaceColor()
    {
        Dog = DeclareSearchableParticipant<Dog>();
        Color = DeclareSearchableParticipant<Color>();
        Name = DeclareUniqueSearchableParticipant<string>();
        DogColor = DeclareComposite(Dog, Color);
        NameColor = DeclareOrderedComposite(Name, Color);
        Sign = Conclude(Dog, Name, Color);
    }

    public SearchableParticipant<Dog> Dog { get; }
    public SearchableParticipant<Color> Color { get; }

    public UniqueSearchableParticipant<string> Name { get; }

    public Composite<(Dog, Color)> DogColor { get; }

    public OrderedComposite<(string, Color)> NameColor { get; }
    protected override Signature Sign { get; }
}