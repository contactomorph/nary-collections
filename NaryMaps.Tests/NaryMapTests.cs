using System.Drawing;
using NaryMaps.Tests.Resources.Data;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests;

public class NaryMapTests
{
    [Test]
    public void CreateAndManipulateBasicMap()
    {
        var map = NaryMap.New<DogPlaceColor>();

        map.AsSet().UnionWith(DogPlaceColorTuples.Data);
        
        var item = map.AsSet().FirstOrDefault();
        
        Assert.That(item, Is.EqualTo((Dogs.KnownDogs[0], "Lyon", Color.Beige)));

        var dog = map.AsSet().Select(s => s.Dog).FirstOrDefault();
        
        Assert.That(dog, Is.EqualTo(Dogs.KnownDogs[0]));

        var names = map.AsSet().Select(s => s.Place).ToArray();
        
        Assert.That(names, Is.EqualTo(new[] { "Lyon", "Paris", "Bordeaux" }));

        var swappedItem = map
            .AsSet()
            .Skip(2)
            .Select(p => (p.Dog, p.Place) as (Dog Dog, string Place)?)
            .FirstOrDefault();
        
        Assert.That(swappedItem, Is.EqualTo((Dogs.KnownDogs[1], "Bordeaux")));
    }

    [Test]
    public void ProjectingMap()
    {
        var map = NaryMap.New<DogPlaceColor>();

        map.AsSet().UnionWith(DogPlaceColorTuples.Data);

        var nameSet = map.AsReadOnlySet(s => s.Name);

        Assert.That(nameSet.Count, Is.EqualTo(map.AsReadOnlySet().Count));

        Assert.That(nameSet.ToList(), 
            Is.EquivalentTo(new[] { "Lyon", "Paris", "Bordeaux" }));

        var dogToTuple = map.With(s => s.Dog).AsReadOnlyMultiDictionary();

        var expectedDogDictionary = map.AsSet()
            .GroupBy(t => t.Dog)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.That(dogToTuple.Count, Is.EqualTo(expectedDogDictionary.Count));
        Assert.That(dogToTuple.ToList(), Is.EquivalentTo(expectedDogDictionary));

        var nameToTuple = map.With(s => s.Name).AsReadOnlyDictionary();

        var expectedNameDictionary = map.AsSet().ToDictionary(t => t.Place);

        Assert.That(nameToTuple.Count, Is.EqualTo(expectedNameDictionary.Count));
        Assert.That(nameToTuple.ToList(), Is.EquivalentTo(expectedNameDictionary));
    }
}