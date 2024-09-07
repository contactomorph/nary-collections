using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryMaps.Components;
using NaryMaps.Implementation;
using NaryMaps.Primitives;
using NaryMaps.Tests.Resources.Data;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);

public class SelectionCompilationTests
{
    private ModuleBuilder _moduleBuilder;
    private static readonly IEqualityComparer<Dog> DogComparer = EqualityComparer<Dog>.Default;
    private static readonly IEqualityComparer<string> StringComparer = EqualityComparer<string>.Default;
    private static readonly IEqualityComparer<Color> ColorComparer = EqualityComparer<Color>.Default;
    
    private static DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)> DataEntry = new()
    {
        DataTuple = (Dogs.KnownDogs[1], "Toulouse", Color.LightSalmon),
        HashTuple = (4, 5, 7),
        BackIndexesTuple = (0, new MultiIndex()),
    };
    
    [SetUp]
    public void Setup()
    {
        AssemblyName assembly = new AssemblyName { Name = "nary_map_test_3_.dll" };
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule("nary_map_module_test");
    }
    
    [Test]
    public void InnerMethodForColorDogTest()
    {
        var dataTypeProjection = new DataTypeProjection(
            typeof(DogPlaceColorTuple),
            1,
            [false, true], // (int, MultiIndex)
            [2, 0]); // (Color, Dog)

        var handlerField = typeof(NaryMapCore).GetField(nameof(NaryMapCore._compositeHandlerA))!;
        
        var ctor = SelectionCompilation.GenerateConstructor(
            _moduleBuilder,
            dataTypeProjection,
            typeof(ColorDogSelection),
            handlerField);
        
        var factory = CreateSelectionFactory<ColorDogSelection>(ctor);

        var comparerTuple = (DogComparer, StringComparer, ColorComparer);

        var map = new NaryMapCore(comparerTuple);

        var selection = factory(map);

        var hc = selection.ComputeHashCode(comparerTuple, (Color.AntiqueWhite, Dogs.KnownDogs[2]));

        var expectedHc = (uint)
            (ColorComparer.GetHashCode(Color.AntiqueWhite), DogComparer.GetHashCode(Dogs.KnownDogs[2]))
            .GetHashCode();
        
        Assert.That(hc, Is.EqualTo(expectedHc));

        var (color, dog) = selection.GetItem(DataEntry);
        
        Assert.That(color, Is.EqualTo(Color.LightSalmon));
        Assert.That(dog, Is.EqualTo(Dogs.KnownDogs[1]));
        
        var dataTuple = selection.GetDataTuple(DataEntry);
        
        Assert.That(dataTuple, Is.EqualTo(DataEntry.DataTuple));

        var handler = selection.GetHandler();
        
        Assert.That(handler, Is.EqualTo(map._compositeHandlerA));
        Assert.That(handler.GetHashEntryCount(), Is.EqualTo(-1));
        Assert.That(handler.GetHashTable().Length, Is.EqualTo(30));
    }

    [Test]
    public void InnerMethodForPlaceTest()
    {
        var dataTypeProjection = new DataTypeProjection(
            typeof(DogPlaceColorTuple),
            0,
            [false, true], // (int, MultiIndex)
            [1]); // Place
        
        var handlerField = typeof(NaryMapCore).GetField(nameof(NaryMapCore._compositeHandlerB))!;
        
        var ctor = SelectionCompilation.GenerateConstructor(
            _moduleBuilder,
            dataTypeProjection,
            typeof(PlaceSelection),
            handlerField);
        
        var factory = CreateSelectionFactory<PlaceSelection>(ctor);

        var comparerTuple = (DogComparer, StringComparer, ColorComparer);

        var map = new NaryMapCore(comparerTuple);

        var selection = factory(map);

        var hc = selection.ComputeHashCode(comparerTuple, "Paris");

        var expectedHc = (uint)"Paris".GetHashCode();
        
        Assert.That(hc, Is.EqualTo(expectedHc));
        
        var place = selection.GetItem(DataEntry);
        
        Assert.That(place, Is.EqualTo("Toulouse"));
        
        var dataTuple = selection.GetDataTuple(DataEntry);
        
        Assert.That(dataTuple, Is.EqualTo(DataEntry.DataTuple));
        
        var handler = selection.GetHandler();
        
        Assert.That(handler, Is.EqualTo(map._compositeHandlerB));
        Assert.That(handler.GetHashEntryCount(), Is.EqualTo(42));
        Assert.That(handler.GetHashTable().Length, Is.EqualTo(100));
    }

    private static Func<NaryMapCore, TSelection> CreateSelectionFactory<TSelection>(ConstructorInfo ctor)
    {
        var parameter = Expression.Parameter(typeof(NaryMapCore));
        
        var lambda = Expression.Lambda<Func<NaryMapCore, TSelection>>(Expression.New(ctor, parameter), parameter);
        
        return lambda.Compile();
    }
}