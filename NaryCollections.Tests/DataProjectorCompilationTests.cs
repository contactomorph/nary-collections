using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using ColorPlaceTuple = (Color Color, string Place);
using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, MultiIndex)>;

public class DataProjectorCompilationTests
{
    private ModuleBuilder _moduleBuilder;
    private static readonly IEqualityComparer<Dog> DogComparer = EqualityComparer<Dog>.Default;
    private static readonly IEqualityComparer<string> StringComparer = EqualityComparer<string>.Default;
    private static readonly IEqualityComparer<Color> ColorComparer = EqualityComparer<Color>.Default;

    private static object CallDogCtor(ConstructorInfo projectorConstructor)
    {
        var lambda = Expression
            .Lambda(Expression.New(projectorConstructor, Expression.Constant(false)))
            .Compile();
        
        return lambda.DynamicInvoke()!;
    }
    
    private static object CallColorPlaceCtor(ConstructorInfo projectorConstructor)
    {
        var lambda = Expression
            .Lambda(Expression.New(projectorConstructor, Expression.Constant(false)))
            .Compile();
        
        return lambda.DynamicInvoke()!;
    }
    
    private static object CallDogPlaceColorCtor(ConstructorInfo projectorConstructor)
    {
        var lambda = Expression
            .Lambda(Expression.New(projectorConstructor, Expression.Constant(false)))
            .Compile();
        
        return lambda.DynamicInvoke()!;
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
        
        var constructor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0],
            0,
            [false, true]);
        
        var handler = CallDogCtor(constructor);
        var resizeHandler = (IResizeHandler<DogPlaceColorEntry>)handler;
        var dataEquator = (IDataEquator<DogPlaceColorEntry, ComparerTuple, Dog>)resizeHandler;
        var comparerTuple = (DogComparer, StringComparer, ColorComparer);

        for (int i = 0; i < dataTable.Length; ++i)
        {
            var expectedDog = dataTable[i].DataTuple.Dog;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (expectedDog is null)
                continue;
            var expectedDogHashCode = (uint)expectedDog.GetHashCode();
            
            var hashCode = resizeHandler.GetHashCodeAt(dataTable, i);
            Assert.That(hashCode, Is.EqualTo(expectedDogHashCode));

            var dog = dataTable[i].DataTuple.Dog;
            Assert.IsTrue(dataEquator.AreDataEqualAt(dataTable, comparerTuple, i, dog, hashCode));
            
            if (0 < i && dog != dataTable[i - 1].DataTuple.Dog)
            {
                Assert.IsFalse(dataEquator.AreDataEqualAt(dataTable, comparerTuple, i - 1, dog, hashCode));
            }
        }
    }
    
    [Test]
    public void ScalarGetAndEqualAtTest2()
    {
        DogPlaceColorGeneration.CreateDataTableOnly(DogPlaceColorTuples.Data, out var dataTable);
        
        var ctor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0],
            0,
            [false, true]);

        var del = Expression.Lambda(Expression.New(ctor, Expression.Constant(true))).Compile();

        var handler = (IDataEquator<DogPlaceColorEntry, ComparerTuple, Dog>)del.DynamicInvoke()!;
        
        var comparerTuple = (DogComparer, StringComparer, ColorComparer);
        
        for (int i = 0; i < dataTable.Length; ++i)
        {
            var dataTuple = dataTable[i].DataTuple;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (dataTuple.Dog is null)
                continue;
            var expectedDogHashCode = (uint)dataTuple.Dog.GetHashCode();
            
            Assert.IsTrue(handler.AreDataEqualAt(dataTable, comparerTuple, i, dataTuple.Dog, expectedDogHashCode));
        }
    }
    
    [Test]
    public void CompositeGetAndEqualAtTest()
    {
        DogPlaceColorGeneration.CreateDataTableOnly(DogPlaceColorTuples.Data, out var dataTable);
        
        var constructor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [2, 1],
            0,
            [false, true]);
        
        var handler = CallColorPlaceCtor(constructor);
        var resizeHandler = (IResizeHandler<DogPlaceColorEntry>)handler;
        var dataEquator = (IDataEquator<DogPlaceColorEntry, ComparerTuple, ColorPlaceTuple>)handler;
        var comparerTuple = (DogComparer, StringComparer, ColorComparer);
    
        for (int i = 0; i < dataTable.Length; ++i)
        {
            var dataTuple = dataTable[i].DataTuple;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (dataTuple.Dog is null)
                continue;
            var expectedTupleHashCode = (uint)
                ((uint)dataTuple.Color.GetHashCode(), (uint)dataTuple.Place.GetHashCode()).GetHashCode();
            
            var hashCode = resizeHandler.GetHashCodeAt(dataTable, i);
            
            Assert.That(hashCode, Is.EqualTo(expectedTupleHashCode));
    
            var tuple = (dataTuple.Color, dataTuple.Place);
            Assert.IsTrue(dataEquator.AreDataEqualAt(dataTable, comparerTuple, i, tuple, hashCode));
        }
    }
    
    [Test]
    public void CompleteGetAndEqualAtTest()
    {
        DogPlaceColorGeneration.CreateDataTableOnly(DogPlaceColorTuples.Data, out var dataTable);
        
        var constructor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0, 1, 2],
            0,
            [false, true]);
        
        var handler = CallDogPlaceColorCtor(constructor);
        var resizeHandler = (IResizeHandler<DogPlaceColorEntry>)handler;
        var dataEquator = (IDataEquator<DogPlaceColorEntry, ComparerTuple, DogPlaceColorTuple>)handler;
        var comparerTuple = (DogComparer, StringComparer, ColorComparer);
    
        for (int i = 0; i < dataTable.Length; ++i)
        {
            var dataTuple = dataTable[i].DataTuple;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (dataTuple.Dog is null)
                continue;
            var expectedTupleHashCode = (uint)(
                        (uint)dataTuple.Dog.GetHashCode(),
                        (uint)dataTuple.Place.GetHashCode(),
                        (uint)dataTuple.Color.GetHashCode()
                    ).GetHashCode();
            
            var hashCode = resizeHandler.GetHashCodeAt(dataTable, i);
            
            Assert.That(hashCode, Is.EqualTo(expectedTupleHashCode));
    
            var tuple = (dataTuple.Dog, dataTuple.Place, dataTuple.Color);
            Assert.IsTrue(dataEquator.AreDataEqualAt(dataTable, comparerTuple, i, tuple, hashCode));
        }
    }
    
    [Test]
    public void GetAndSetBackIndexTest()
    {
        DogPlaceColorEntry[] dataTable = new DogPlaceColorEntry[12];
        for (int i = 0; i < dataTable.Length; ++i) dataTable[i].BackIndexesTuple.Item1 = (i + 5) % dataTable.Length;
        
        var constructor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder,
            typeof(DogPlaceColorTuple),
            [0],
            0,
            [false, true]);
        
        var resizeHandler = (IResizeHandler<DogPlaceColorEntry>)CallDogCtor(constructor);
        
        for (int i = 0; i < dataTable.Length; ++i)
        {
            var backIndex = resizeHandler.GetBackIndex(dataTable, i);
            Assert.That(backIndex, Is.EqualTo(dataTable[i].BackIndexesTuple.Item1));
        }
        
        for (int i = 0; i < dataTable.Length; ++i)
        {
            int newBackIndex = (i + dataTable.Length - 3) % dataTable.Length;
            resizeHandler.SetBackIndex(dataTable, i, newBackIndex);
            Assert.That(dataTable[i].BackIndexesTuple.Item1, Is.EqualTo(newBackIndex));
        }
    }
}