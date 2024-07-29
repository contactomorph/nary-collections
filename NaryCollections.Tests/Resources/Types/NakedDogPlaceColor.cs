using System.Drawing;

namespace NaryCollections.Tests.Resources.Types;

public sealed class NakedDogPlaceColor : Schema<(Dog Dog, string Place, Color Color)>
{
    public NakedDogPlaceColor()
    {
        Dog = AddParticipant<Dog>();
        Color = AddParticipant<Color>();
        Name = AddParticipant<string>();
        Sign = Conclude(Dog, Name, Color);
    }

    public Participant<Dog> Dog { get; }
    public Participant<Color> Color { get; }
    public Participant<string> Name { get; }
    protected override Signature Sign { get; }
}