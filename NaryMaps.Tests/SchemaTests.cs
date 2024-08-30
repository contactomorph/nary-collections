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
            new (false, 0, [schema.Dog]),
            new (false, 1, [schema.Color]),
            new (true, 2, [schema.Name]),
            new (false, 3, [schema.Dog, schema.Color]),
            new (false, 4, [schema.Name, schema.Color]),
        }));
    }
}