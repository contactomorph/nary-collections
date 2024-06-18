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

    private static IDataProjector<DogPlaceColorEntry, Dog> CallDogCtor(
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
    
    private static IDataProjector<DogPlaceColorEntry, ColorPlaceTuple> CallColorPlaceCtor(
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
    
    private static ICompleteDataProjector<DogPlaceColorTuple, (uint, uint, uint), ValueTuple<int>> CallDogPlaceColorCtor(
        ConstructorInfo projectorConstructor)
    {
        var parameters = projectorConstructor
            .GetParameters()
            .Select((p, i) => Expression.Parameter(p.ParameterType, $"comparer{i}"))
            .ToArray();

        var lambda = Expression
            .Lambda(Expression.New(projectorConstructor, parameters), parameters)
            .Compile();
        
        return (ICompleteDataProjector<DogPlaceColorTuple, (uint, uint, uint), ValueTuple<int>>)lambda.DynamicInvoke(
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
    public void ScalarGetAndEqualAtTest()
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
    public void CompositeGetAndEqualAtTest()
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
    public void CompleteGetAndEqualAtTest()
    {
        DogPlaceColorGeneration.CreateDataTableOnly(DogPlaceColorTuples.Data, out var dataTable);
        
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0, 1, 2],
            0,
            1);
        
        var projector = CallDogPlaceColorCtor(constructor);
    
        for (int i = 0; i < dataTable.Length; ++i)
        {
            var dataTuple = dataTable[i].DataTuple;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (dataTuple.Dog is null)
                continue;
            var expectedTuple = (dataTuple.Dog, dataTuple.Place, dataTuple.Color);
            var expectedTupleHashCode = (uint)(
                        (uint)dataTuple.Dog.GetHashCode(),
                        (uint)dataTuple.Place.GetHashCode(),
                        (uint)dataTuple.Color.GetHashCode()
                    ).GetHashCode();
            
            var (tuple, hashCode) = projector.GetDataAt(dataTable, i);
            
            Assert.That(tuple, Is.EqualTo(expectedTuple));
            Assert.That(hashCode, Is.EqualTo(expectedTupleHashCode));
    
            Assert.IsTrue(projector.AreDataEqualAt(dataTable, i, tuple, hashCode));
        }
    }
    
    [Test]
    public void GetAndSetBackIndexTest()
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
    public void ScalarComputeHashCodeTest()
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
        
        var projector2 = CallDogCtor(constructor, new CustomDogEqualityComparer((dog, 4)));
        var hc2 = projector2.ComputeHashCode(dog);
        
        Assert.That(hc2, Is.EqualTo((uint)4));
    }

    [Test]
    public void CompositeComputeHashCodeTest()
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

    [Test]
    public void CompleteComputeHashCodeTest()
    {
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0, 1, 2],
            0,
            1);
        
        var projector = CallDogPlaceColorCtor(constructor);

        var dog = new Dog("Aramis", "Louis");
        var hc = projector.ComputeHashCode((dog, "Rio de Janeiro", Color.LawnGreen));

        var hashTuple = (
            EqualityComparerHandling.ComputeRefHashCode(DogComparer, dog),
            EqualityComparerHandling.ComputeRefHashCode(StringComparer, "Rio de Janeiro"),
            EqualityComparerHandling.ComputeStructHashCode(ColorComparer, Color.LawnGreen));
        
        Assert.That(hc, Is.EqualTo(EqualityComparerHandling.ComputeTupleHashCode(hashTuple)));
    }
    
    [Test]
    public void SetDataAtTest()
    {
        DogPlaceColorGeneration.CreateDataTableOnly(DogPlaceColorTuples.Data, out var dataTable);

        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0, 1, 2],
            0,
            1);
        
        var projector = CallDogPlaceColorCtor(constructor);

        var previousDataTuple = dataTable[3].DataTuple;
        var previousHashTuple = dataTable[3].HashTuple;

        var newDataTuple = dataTable[7].DataTuple;
        var newHashTuple = dataTable[7].HashTuple;
        
        projector.SetDataAt(dataTable, 3, newDataTuple, newHashTuple);
        
        Assert.That(dataTable[3].DataTuple, Is.Not.EqualTo(previousDataTuple).And.EqualTo(newDataTuple));
        Assert.That(dataTable[3].HashTuple, Is.Not.EqualTo(previousHashTuple).And.EqualTo(newHashTuple));
    }
    
    [Test]
    public void ComputeTupleHashTest()
    {
        var constructor = DataProjectorCompilation.GenerateProjectorConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0, 1, 2],
            0,
            1);
        
        var projector = CallDogPlaceColorCtor(constructor);

        foreach (var dataTuple in DogPlaceColorTuples.Data)
        {
            var hashTuple = projector.ComputeHashTuple(dataTuple);
            
            var expectedHashTuple = (
                EqualityComparerHandling.ComputeRefHashCode(DogComparer, dataTuple.Dog),
                EqualityComparerHandling.ComputeRefHashCode(StringComparer, dataTuple.Place),
                EqualityComparerHandling.ComputeStructHashCode(ColorComparer, dataTuple.Color));
            
            Assert.That(hashTuple, Is.EqualTo(expectedHashTuple));
        }
    }
}