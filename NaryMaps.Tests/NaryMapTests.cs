using System.Drawing;
using System.Text;
using NaryMaps.Tests.Resources.Data;
using NaryMaps.Tests.Resources.DataGeneration;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);

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

            var dogToTuple = map.With(s => s.Dog).AsReadOnlyDictionaryOfEnumerable();
            
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

            var colorToTuple = map.With(s => s.Color).AsReadOnlyDictionaryOfEnumerable();
            
            Assert.That(colorToTuple.Count, Is.EqualTo(expectedColorDictionary.Count));
            foreach (var (color, tuples) in colorToTuple)
            {
                Assert.That(tuples, Is.EquivalentTo(expectedColorDictionary[color]));
            }
        }
    }

    [Test]
    public void SetOperationForProjectionsTest()
    {
        var map = NaryMap.New<DogPlaceColor>();

        map.AsSet().UnionWith(DogPlaceColorTuples.DataWithUniquePlace);

        {
            // Color
            Color[] colors = [Color.CadetBlue, Color.Beige, Color.Plum, Color.Orange, Color.Black, Color.BurlyWood, ];
            
            var colorSet = map.AsReadOnlySet(s => s.Color);
            
            Assert.That(colorSet.IsSubsetOf(Array.Empty<Color>()), Is.False);
            Assert.That(colorSet.IsSubsetOf(colors.Take(3)), Is.False);
            Assert.That(colorSet.IsSubsetOf(colors.Take(4)), Is.True);
            Assert.That(colorSet.IsSubsetOf(colors.Take(5)), Is.True);
            Assert.That(colorSet.IsSubsetOf(colors), Is.True);
            Assert.That(colorSet.IsProperSubsetOf(Array.Empty<Color>()), Is.False);
            Assert.That(colorSet.IsProperSubsetOf(colors.Take(3)), Is.False);
            Assert.That(colorSet.IsProperSubsetOf(colors.Take(4)), Is.False);
            Assert.That(colorSet.IsProperSubsetOf(colors.Take(5)), Is.True);
            Assert.That(colorSet.IsProperSubsetOf(colors), Is.True);
            Assert.That(colorSet.IsSupersetOf(Array.Empty<Color>()), Is.True);
            Assert.That(colorSet.IsSupersetOf(colors.Take(3)), Is.True);
            Assert.That(colorSet.IsSupersetOf(colors.Take(4)), Is.True);
            Assert.That(colorSet.IsSupersetOf(colors.Take(5)), Is.False);
            Assert.That(colorSet.IsSupersetOf(colors), Is.False);
            Assert.That(colorSet.IsProperSupersetOf(Array.Empty<Color>()), Is.True);
            Assert.That(colorSet.IsProperSupersetOf(colors.Take(3)), Is.True);
            Assert.That(colorSet.IsProperSupersetOf(colors.Take(4)), Is.False);
            Assert.That(colorSet.IsProperSupersetOf(colors.Take(5)), Is.False);
            Assert.That(colorSet.IsProperSupersetOf(colors), Is.False);
            Assert.That(colorSet.Overlaps(Array.Empty<Color>()), Is.False);
            Assert.That(colorSet.Overlaps(colors.Take(1)), Is.True);
            Assert.That(colorSet.Overlaps(colors.Skip(4)), Is.False);
            Assert.That(colorSet.Overlaps(colors), Is.True);
            Assert.That(colorSet.SetEquals(Array.Empty<Color>()), Is.False);
            Assert.That(colorSet.SetEquals(colors.Take(3)), Is.False);
            Assert.That(colorSet.SetEquals(colors.Take(4)), Is.True);
            Assert.That(colorSet.SetEquals(colors.Take(5)), Is.False);
            Assert.That(colorSet.SetEquals(colors), Is.False);
        }

        {
            // Dog Color
            (Dog, Color)[] dogColors = [
                (Dogs.KnownDogs[0], Color.Beige),
                (Dogs.KnownDogs[1], Color.CadetBlue),
                (Dogs.KnownDogs[2], Color.Beige),
                (Dogs.KnownDogs[2], Color.Plum),
                (Dogs.KnownDogs[1], Color.Orange),
                (Dogs.KnownDogs[0], Color.CadetBlue),
                (Dogs.UnknownDogs[0], Color.Plum),
                (Dogs.KnownDogs[0], Color.Black),
                (Dogs.KnownDogs[0], Color.BurlyWood),
            ];

            var dogColorSet = map.AsReadOnlySet(s => s.DogColor);
            
            Assert.That(dogColorSet.IsSubsetOf(Array.Empty<(Dog, Color)>()), Is.False);
            Assert.That(dogColorSet.IsSubsetOf(dogColors.Take(6)), Is.False);
            Assert.That(dogColorSet.IsSubsetOf(dogColors.Take(7)), Is.True);
            Assert.That(dogColorSet.IsSubsetOf(dogColors.Take(8)), Is.True);
            Assert.That(dogColorSet.IsSubsetOf(dogColors), Is.True);
            Assert.That(dogColorSet.IsProperSubsetOf(Array.Empty<(Dog, Color)>()), Is.False);
            Assert.That(dogColorSet.IsProperSubsetOf(dogColors.Take(6)), Is.False);
            Assert.That(dogColorSet.IsProperSubsetOf(dogColors.Take(7)), Is.False);
            Assert.That(dogColorSet.IsProperSubsetOf(dogColors.Take(8)), Is.True);
            Assert.That(dogColorSet.IsProperSubsetOf(dogColors), Is.True);
            Assert.That(dogColorSet.IsSupersetOf(Array.Empty<(Dog, Color)>()), Is.True);
            Assert.That(dogColorSet.IsSupersetOf(dogColors.Take(6)), Is.True);
            Assert.That(dogColorSet.IsSupersetOf(dogColors.Take(7)), Is.True);
            Assert.That(dogColorSet.IsSupersetOf(dogColors.Take(8)), Is.False);
            Assert.That(dogColorSet.IsSupersetOf(dogColors), Is.False);
            Assert.That(dogColorSet.IsProperSupersetOf(Array.Empty<(Dog, Color)>()), Is.True);
            Assert.That(dogColorSet.IsProperSupersetOf(dogColors.Take(6)), Is.True);
            Assert.That(dogColorSet.IsProperSupersetOf(dogColors.Take(7)), Is.False);
            Assert.That(dogColorSet.IsProperSupersetOf(dogColors.Take(8)), Is.False);
            Assert.That(dogColorSet.IsProperSupersetOf(dogColors), Is.False);
            Assert.That(dogColorSet.Overlaps(Array.Empty<(Dog, Color)>()), Is.False);
            Assert.That(dogColorSet.Overlaps(dogColors.Take(1)), Is.True);
            Assert.That(dogColorSet.Overlaps(dogColors.Skip(7)), Is.False);
            Assert.That(dogColorSet.Overlaps(dogColors), Is.True);
            Assert.That(dogColorSet.SetEquals(Array.Empty<(Dog, Color)>()), Is.False);
            Assert.That(dogColorSet.SetEquals(dogColors.Take(6)), Is.False);
            Assert.That(dogColorSet.SetEquals(dogColors.Take(7)), Is.True);
            Assert.That(dogColorSet.SetEquals(dogColors.Take(8)), Is.False);
            Assert.That(dogColorSet.SetEquals(dogColors), Is.False);
        }
    }

    [Test]
    public void FillDogPlaceColorTupleMapRandomlyTest()
    {
        var map = NaryMap.New<DogPlaceColor>();

        var checker = new DogPlaceColorConsistencyChecker(map);
        
        var set = map.AsSet();
        var referenceSet = new HashSet<DogPlaceColorTuple>();
        var random = new Random(4223023);
        var someColors = ((KnownColor[])Enum.GetValues(typeof(KnownColor)))
            .Take(10)
            .Select(Color.FromKnownColor)
            .ToArray();
        
        for(int i = 0; i < 10000; ++i)
        {
            if (referenceSet.Count < random.Next(500))
            {
                Dog dog = Dogs.AllDogs[random.Next(Dogs.AllDogs.Count)];
                Color color = someColors[random.Next(someColors.Length)];
                string place = GenerateText(random);
                DogPlaceColorTuple tuple = (dog, place, color);
                referenceSet.Add(tuple);
                set.Add(tuple);
            }
            else
            {
                var tuple = referenceSet.Skip(random.Next(referenceSet.Count)).First();
                referenceSet.Remove(tuple);
                set.Remove(tuple);
            }
            
            checker.CheckConsistency(map);
        }
        
        while (referenceSet.Count > 0)
        {
            var tuple = referenceSet.First();
            referenceSet.Remove(tuple);
            Assert.IsTrue(set.Remove(tuple));
            
            checker.CheckConsistency(map);
        }
        Assert.That(set, Is.Empty);
    }

    private string GenerateText(Random random)
    {
        StringBuilder sb = new();
        for (int i = 0; i < 50; ++i)
        {
            char c = (char)('a' + random.Next(0, 26));
            sb.Append(c);
        }

        return sb.ToString();
    }
}