using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using ColorPlaceTuple = (Color Color, string Place);
using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

public class DataProjectorCompilationTests
{
    private ModuleBuilder _moduleBuilder;
    private static readonly IEqualityComparer<Dog> DogComparer = EqualityComparer<Dog>.Default;
    private static readonly IEqualityComparer<string> StringComparer = EqualityComparer<string>.Default;
    private static readonly IEqualityComparer<Color> ColorComparer = EqualityComparer<Color>.Default;

    public static IDataProjector<DogPlaceColorEntry, Dog> CallDogCtor(
        ConstructorInfo projectorConstructor,
        IEqualityComparer<Dog>? dogComparer = null)
    {
        var parameters = projectorConstructor
            .GetParameters()
            .Select((p, i) => Expression.Parameter(p.ParameterType, $"comparer{i}"))
            .ToArray();

        var lambda = Expression
            .Lambda(Expression.New(projectorConstructor, parameters), parameters)
            .Compile();
        
        return (IDataProjector<DogPlaceColorEntry, Dog>)lambda.DynamicInvoke(
            dogComparer ?? DogComparer,
            StringComparer,
            ColorComparer)!;
    }
    
    public static IDataProjector<DogPlaceColorEntry, ColorPlaceTuple> CallColorPlaceCtor(
        ConstructorInfo projectorConstructor)
    {
        var parameters = projectorConstructor
            .GetParameters()
            .Select((p, i) => Expression.Parameter(p.ParameterType, $"comparer{i}"))
            .ToArray();

        var lambda = Expression
            .Lambda(Expression.New(projectorConstructor, parameters), parameters)
            .Compile();
        
        return (IDataProjector<DogPlaceColorEntry, ColorPlaceTuple>)lambda.DynamicInvoke(
            DogComparer,
            StringComparer,
            ColorComparer)!;
    }

    [SetUp]
    public void Setup()
    {
        AssemblyName assembly = new AssemblyName { Name = "nary_collection_test_.dll" };
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule("nary_collection_module_test");
    }
    
    [Test]
    public void CheckGetAndEqualAtTest()
    {
        DogPlaceColorGeneration.CreateDataTableOnly(DogPlaceColorTuples.Data, out var dataTable);
        
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0],
            0,
            1);
        
        var projector = CallDogCtor(constructor);

        for (int i = 0; i < dataTable.Length; ++i)
        {
            var expectedDog = dataTable[i].DataTuple.Dog;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (expectedDog is null)
                continue;
            var expectedDogHashCode = (uint)expectedDog.GetHashCode();
            
            var (dog, hashCode) = projector.GetDataAt(dataTable, i);
            
            Assert.That(dog, Is.EqualTo(expectedDog));
            Assert.That(hashCode, Is.EqualTo(expectedDogHashCode));

            Assert.IsTrue(projector.AreDataEqualAt(dataTable, i, dog, hashCode));
            
            if (0 < i && dog != dataTable[i - 1].DataTuple.Dog)
            {
                Assert.IsFalse(projector.AreDataEqualAt(dataTable, i - 1, dog, hashCode));
            }
        }
    }
    
    [Test]
    public void ComplexCheckGetAndEqualAtTest()
    {
        DogPlaceColorGeneration.CreateDataTableOnly(DogPlaceColorTuples.Data, out var dataTable);
        
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [2, 1],
            0,
            1);
        
        var projector = CallColorPlaceCtor(constructor);
    
        for (int i = 0; i < dataTable.Length; ++i)
        {
            var dataTuple = dataTable[i].DataTuple;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (dataTuple.Dog is null)
                continue;
            var expectedTuple = (dataTuple.Color, dataTuple.Place);
            var expectedTupleHashCode = (uint)
                ((uint)dataTuple.Color.GetHashCode(), (uint)dataTuple.Place.GetHashCode()).GetHashCode();
            
            var (tuple, hashCode) = projector.GetDataAt(dataTable, i);
            
            Assert.That(tuple, Is.EqualTo(expectedTuple));
            Assert.That(hashCode, Is.EqualTo(expectedTupleHashCode));
    
            Assert.IsTrue(projector.AreDataEqualAt(dataTable, i, tuple, hashCode));
        }
    }
    
    [Test]
    public void CheckGetAndSetBackIndexTest()
    {
        DogPlaceColorEntry[] dataTable = new DogPlaceColorEntry[12];
        for (int i = 0; i < dataTable.Length; ++i) dataTable[i].BackIndexesTuple.Item1 = (i + 5) % dataTable.Length;
        
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0],
            0,
            1);
        
        var projector = CallDogCtor(constructor);
        
        for (int i = 0; i < dataTable.Length; ++i)
        {
            var backIndex = projector.GetBackIndex(dataTable, i);
            Assert.That(backIndex, Is.EqualTo(dataTable[i].BackIndexesTuple.Item1));
        }
        
        for (int i = 0; i < dataTable.Length; ++i)
        {
            int newBackIndex = (i + dataTable.Length - 3) % dataTable.Length;
            projector.SetBackIndex(dataTable, i, newBackIndex);
            Assert.That(dataTable[i].BackIndexesTuple.Item1, Is.EqualTo(newBackIndex));
        }
    }

    [Test]
    public void ComputeHashCodeTest()
    {
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0],
            0,
            1);
        
        var projector = CallDogCtor(constructor);

        var dog = new Dog("Portos", "Régis");
        var hc = projector.ComputeHashCode(dog);
        
        Assert.That(hc, Is.EqualTo((uint)dog.GetHashCode()));
        
        var projector2 = CallDogCtor(constructor, new CustomDogEqualityComparer());
        var hc2 = projector2.ComputeHashCode(dog);
        
        Assert.That(hc2, Is.EqualTo((uint)4));
    }

    [Test]
    public void ComplexComputeHashCodeTest()
    {
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [2, 1],
            0,
            1);
        
        var projector = CallColorPlaceCtor(constructor);

        var hc = projector.ComputeHashCode((Color.DarkViolet, "Tokyo"));

        var hashTuple = (
            EqualityComparerHandling.ComputeStructHashCode(ColorComparer, Color.DarkViolet),
            EqualityComparerHandling.ComputeRefHashCode(StringComparer, "Tokyo"));
        
        Assert.That(hc, Is.EqualTo(EqualityComparerHandling.ComputeTupleHashCode(hashTuple)));
    }

    private sealed class CustomDogEqualityComparer : IEqualityComparer<Dog>
    {
        public bool Equals(Dog? x, Dog? y) => throw new NotImplementedException();

        public int GetHashCode(Dog obj) => 4;
    }
}