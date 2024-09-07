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

        {
            // Name
            var expectedNameDictionary = map.AsSet().ToDictionary(t => t.Place);

            var nameSet = map.AsReadOnlySet(s => s.Name);
            
            Assert.That(nameSet.Count, Is.EqualTo(expectedNameDictionary.Count));
            Assert.That(nameSet.ToList(), Is.EquivalentTo(expectedNameDictionary.Keys));

            var nameToTuple = map.With(s => s.Name).AsReadOnlyDictionary();

            Assert.That(nameToTuple.Count, Is.EqualTo(expectedNameDictionary.Count));
            Assert.That(nameToTuple.ToList(), Is.EquivalentTo(expectedNameDictionary));
        }
        {
            // Dog
            var expectedDogDictionary = map.AsSet()
                .GroupBy(t => t.Dog)
                .ToDictionary(g => g.Key, g => g.ToList());

            var dogSet = map.AsReadOnlySet(s => s.Dog);
            
            Assert.That(dogSet.Count, Is.EqualTo(expectedDogDictionary.Count));
            Assert.That(dogSet.ToList(), Is.EquivalentTo(expectedDogDictionary.Keys));

            var dogToTuple = map.With(s => s.Dog).AsReadOnlyMultiDictionary();
            
            Assert.That(dogToTuple.Count, Is.EqualTo(expectedDogDictionary.Count));
            foreach (var (dog, tuples) in dogToTuple)
            {
                Assert.That(tuples, Is.EquivalentTo(expectedDogDictionary[dog]));
            }
        }
        {
            // Color
            var expectedColorDictionary = map.AsReadOnlySet()
                .GroupBy(t => t.Color)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            var colorSet = map.AsReadOnlySet(s => s.Color);

            Assert.That(colorSet.Count, Is.EqualTo(expectedColorDictionary.Count));
            Assert.That(colorSet.ToList(), Is.EquivalentTo(expectedColorDictionary.Keys));

            var colorToTuple = map.With(s => s.Color).AsReadOnlyMultiDictionary();
            
            Assert.That(colorToTuple.Count, Is.EqualTo(expectedColorDictionary.Count));
            foreach (var (color, tuples) in colorToTuple)
            {
                Assert.That(tuples, Is.EquivalentTo(expectedColorDictionary[color]));
            }
        }
    }
}