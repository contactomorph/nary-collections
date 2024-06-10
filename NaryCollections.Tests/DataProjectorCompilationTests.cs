
using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Details;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

public class DataProjectorCompilationTests
{
    private ModuleBuilder _moduleBuilder;
    private static readonly IEqualityComparer<Dog> DogComparer = EqualityComparer<Dog>.Default;
    private static readonly IEqualityComparer<string> StringComparer = EqualityComparer<string>.Default;
    private static readonly IEqualityComparer<Color> ColorComparer = EqualityComparer<Color>.Default;

    public static IDataProjector<DogPlaceColorEntry, Dog> Call(ConstructorInfo projectorConstructor)
    {
        var parameters = projectorConstructor
            .GetParameters()
            .Select((p, i) => Expression.Parameter(p.ParameterType, $"comparer{i}"))
            .ToArray();

        var lambda = Expression
            .Lambda(Expression.New(projectorConstructor, parameters), parameters)
            .Compile();
        
        return (IDataProjector<DogPlaceColorEntry, Dog>)lambda.DynamicInvoke(
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
        
        var projector = Call(constructor);

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
        
        var projector = Call(constructor);
        
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
}