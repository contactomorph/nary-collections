using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Tools;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using HashTuple = (uint, uint, uint);
using BackIndexTuple = ValueTuple<int>;
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), ValueTuple<int>>;

public class CompositeHandlerCompilationTests
{
    private ModuleBuilder _moduleBuilder;

    [SetUp]
    public void Setup()
    {
        AssemblyName assembly = new AssemblyName { Name = "composite_handler_.dll" };
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule("nary_collection_module_test");
    }

    [Test]
    public void CanCreateTest()
    {
        var ctor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder, 
            typeof(DogPlaceColorTuple),
            [0],
            0,
            1);
        
        var del = Expression.Lambda(Expression.New(ctor, Expression.Constant(true))).Compile();

        var untypedHandler =  del.DynamicInvoke();
        Assert.That(
            untypedHandler,
            Is.Not.Null
                .And.InstanceOf<ICompositeHandler<DogPlaceColorTuple, HashTuple, BackIndexTuple, ComparerTuple, Dog>>());
    }

    [Test]
    public void AddTest()
    {
        var ctor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder, 
            typeof(DogPlaceColorTuple),
            [0],
            0,
            1);
        
        var del = Expression.Lambda(Expression.New(ctor, Expression.Constant(true))).Compile();

        var handler =
            (ICompositeHandler<DogPlaceColorTuple, HashTuple, BackIndexTuple, ComparerTuple, Dog>)
            del.DynamicInvoke()!;

        int dataCount = 0;
        var dataTable = new DataEntry<DogPlaceColorTuple, HashTuple, BackIndexTuple>[10];
        
        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode);
        var dogPlaceColorProjector = new DogPlaceColorProjector(dogComparer);
        var dogProjector = new DogProjector(dogComparer);

        var manipulator = FieldManipulator.ForRealTypeOf(handler);
        var hashTableGetter = manipulator.CreateGetter<HashEntry[]>("_hashTable");
        
        Assert.That(
            hashTableGetter(handler).Length,
            Is.EqualTo(HashEntry.TableMinimalLength));
        
        foreach (var (dog, hc) in Dogs.KnownDogsWithHashCode)
        {
            var tuple = (dog, "Montevideo", Color.Thistle);
            var hashTuple = dogPlaceColorProjector.ComputeHashTuple(tuple);
        
            Assert.That(hashTuple.Item1, Is.EqualTo(hc));

            var lastSearchResult = MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>.ContainsForUnique(
                hashTableGetter(handler),
                dataTable,
                dogProjector,
                (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                hc,
                dog);

            Assert.That(lastSearchResult.Case, Is.Not.EqualTo(SearchCase.ItemFound));
        
            var candidateDataIndex = DataHandling<DogPlaceColorTuple, HashTuple, BackIndexTuple>.AddOnlyData(
                ref dataTable,
                tuple,
                hashTuple,
                ref dataCount);
        
            handler.Add(dataTable, lastSearchResult, candidateDataIndex, dataCount);

            Consistency.CheckForUnique(
                hashTableGetter(handler),
                dataTable,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }

        Assert.That(
            hashTableGetter(handler).Length,
            Is.EqualTo(HashEntry.IncreaseCapacity(HashEntry.TableMinimalLength)));
    }
    
    [Test]
    public void RemoveTest()
    {
        var ctor = CompositeHandlerCompilation.GenerateConstructor(
            _moduleBuilder, 
            typeof(DogPlaceColorTuple),
            [0],
            0,
            1);
        
        var del = Expression.Lambda(Expression.New(ctor, Expression.Constant(true))).Compile();

        var handler =
            (ICompositeHandler<DogPlaceColorTuple, HashTuple, BackIndexTuple, ComparerTuple, Dog>)
            del.DynamicInvoke()!;

        var data = Dogs.KnownDogsWithHashCode
            .Select(dh => (dh.Dog, "Berlin", Color.Yellow))
            .ToList();

        var dogComparer = new CustomDogEqualityComparer(Dogs.KnownDogsWithHashCode.Concat(Dogs.NewDogsWithHashCode));
        var dogPlaceColorProjector = new DogPlaceColorProjector(dogComparer);
        var dogProjector = new DogProjector(dogComparer);
        
        DogPlaceColorGeneration.CreateTablesForUnique(
            data,
            out var hashTable,
            out var dataTable,
            hashTuple => hashTuple.Item1,
            dataTuple => dataTuple.Dog,
            dogPlaceColorProjector);
        
        int dataCount = data.Count;
        
        var manipulator = FieldManipulator.ForRealTypeOf(handler);
        manipulator.SetFieldValue(handler, "_hashTable", hashTable);
        var hashTableGetter = manipulator.CreateGetter<HashEntry[]>("_hashTable");

        foreach (var (dog, hc) in Dogs.KnownDogsWithHashCode)
        {
            var tuple = (dog, "Montevideo", Color.Thistle);
            var hashTuple = dogPlaceColorProjector.ComputeHashTuple(tuple);

            Assert.That(hashTuple.Item1, Is.EqualTo(hc));

            hashTable = hashTableGetter(handler);
            var successfulSearchResult =
                MembershipHandling<DogPlaceColorEntry, ComparerTuple, Dog, DogProjector>
                    .ContainsForUnique(
                        hashTable,
                        dataTable,
                        dogProjector,
                        (dogComparer, EqualityComparer<string>.Default, EqualityComparer<Color>.Default),
                        hc,
                        dog);

            Assert.That(successfulSearchResult.Case, Is.EqualTo(SearchCase.ItemFound));

            var dataIndex = hashTable[successfulSearchResult.ReducedHashCode].ForwardIndex;
            
            DataHandling<DogPlaceColorTuple, HashTuple, BackIndexTuple>.RemoveOnlyData(
                ref dataTable,
                dataIndex,
                ref dataCount);
        
            handler.Remove(dataTable, successfulSearchResult, dataCount);

            Consistency.CheckForUnique(
                hashTableGetter(handler),
                dataTable,
                dataCount,
                dogPlaceColorProjector,
                dogPlaceColorProjector.ComputeHashTuple);
        }
    }
}