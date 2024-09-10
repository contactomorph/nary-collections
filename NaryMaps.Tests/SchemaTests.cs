using System.Collections.Immutable;
using System.Drawing;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests;

public class SchemaTests
{
    [Test]
    public void DogPlaceColoSchemaTest()
    {
        var schema = new DogPlaceColor();
        
        Assert.That(schema.DataTupleType, Is.EqualTo(typeof((Dog, string, Color))));

        var composites = schema.GetComposites();
        
        Assert.That(composites, Is.EquivalentTo(new Schema.Composite[]
        {
            new (false, 0, ImmutableArray.Create<IParticipant>(schema.Dog)),
            new (false, 1, ImmutableArray.Create<IParticipant>(schema.Color)),
            new (true, 2, ImmutableArray.Create<IParticipant>(schema.Name)),
            new (false, 3, ImmutableArray.Create<IParticipant>(schema.Dog, schema.Color)),
            new (false, 4, ImmutableArray.Create<IParticipant>(schema.Name, schema.Color)),
        }));
    }
}