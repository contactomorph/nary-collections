using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Implementation;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Data;
using NaryCollections.Tests.Resources.DataGeneration;
using NaryCollections.Tests.Resources.Tools;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using HashTuple = (uint, uint, uint);
using IndexTuple = (int, int, int, int, int);
using DogPlaceColorEntry = DataEntry<(Dog Dog, string Place, Color Color), (uint, uint, uint), (int, int, int, int, int)>;

public class NaryCollectionCompilationTests
{
    private ModuleBuilder _moduleBuilder;
    
    [SetUp]
    public void Setup()
    {
        AssemblyName assembly = new AssemblyName { Name = "nary_collection_test_2_.dll" };
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule("nary_collection_module_test");
    }
    
    [Test]
    public void CompileDogPlaceColorTupleCollectionTest()
    {
        var (_, factory) = NaryCollectionCompilation<DogPlaceColor>.GenerateCollectionConstructor(_moduleBuilder);

        var collection = factory();
        
        var set = collection.AsSet();
        
        Assert.That(set.Count, Is.EqualTo(0));
        
        var array = set.ToArray();
        Assert.That(array.Length, Is.EqualTo(0));
        
        Assert.That(set.Add(DogPlaceColorTuples.Data[0]), Is.True);
        Assert.That(set.Contains(DogPlaceColorTuples.Data[0]), Is.True);
        Assert.That(set.Contains(DogPlaceColorTuples.Data[1]), Is.False);
        Assert.That(set.Count, Is.EqualTo(1));
        
        Assert.That(set.Add(DogPlaceColorTuples.Data[0]), Is.False);
        Assert.That(set.Count, Is.EqualTo(1));
        
        Assert.That(set.Add(DogPlaceColorTuples.Data[1]), Is.True);
        Assert.That(set.Add(DogPlaceColorTuples.Data[2]), Is.True);
        Assert.That(set.Add(DogPlaceColorTuples.Data[3]), Is.True);
        Assert.That(set.Count, Is.EqualTo(4));
        
        array = set.ToArray();
        Assert.That(array, Is.EqualTo(DogPlaceColorTuples.Data.Take(4)));

        void ModifyInLoop()
        {
            int i = 0;
            foreach (var _ in set)
            {
                if (i++ == 2)
                {
                    set.Add(DogPlaceColorTuples.Data[4]);
                }
            }
        }

        Assert.That(ModifyInLoop, Throws.Exception);
        Assert.That(set.Count, Is.EqualTo(5));

        Assert.That(set.Remove(DogPlaceColorTuples.Data[5]), Is.False);
        Assert.That(set.Count, Is.EqualTo(5));
        
        Assert.That(set.Contains(DogPlaceColorTuples.Data[3]), Is.True);
        Assert.That(set.Remove(DogPlaceColorTuples.Data[3]), Is.True);
        Assert.That(set.Contains(DogPlaceColorTuples.Data[3]), Is.False);
        Assert.That(set.Count, Is.EqualTo(4));
    }
    
    [Test]
    public void FillDogPlaceColorTupleCollectionRandomlyTest()
    {
        var (_, factory) = NaryCollectionCompilation<DogPlaceColor>.GenerateCollectionConstructor(_moduleBuilder);

        var collection = factory();
        
        var manipulator = FieldManipulator.ForRealTypeOf(collection);

        var resizeHandlerGetter = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry>>(
            "_compositeHandler");

        var resizeHandler = resizeHandlerGetter(collection);
        
        var handlerManipulator = FieldManipulator.ForRealTypeOf(resizeHandler);
        
        var hashTableGetter = handlerManipulator.CreateGetter<HashEntry[]>("_hashTable");
        var dataTableGetter = manipulator
            .CreateGetter<DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[]>("_dataTable");

        var set = collection.AsSet();
        var referenceSet = new HashSet<DogPlaceColorTuple>();
        var random = new Random(4223023);
        var someColors = Enum.GetValues<KnownColor>().Take(10).Select(Color.FromKnownColor).ToArray();
        
        for(int i = 0; i < 10000; ++i)
        {
            if (referenceSet.Count < random.Next(500))
            {
                Dog dog = Dogs.AllDogs[random.Next(Dogs.AllDogs.Count)];
                Color color = someColors[random.Next(someColors.Length)];
                DogPlaceColorTuple tuple = (dog, "France", color);
                referenceSet.Add(tuple);
                set.Add(tuple);
            }
            else
            {
                var tuple = referenceSet.Skip(random.Next(referenceSet.Count)).First();
                referenceSet.Remove(tuple);
                set.Remove(tuple);
            }
            
            resizeHandler = resizeHandlerGetter(collection);
            
            var hashTable = hashTableGetter(resizeHandler);
            var dataTable = dataTableGetter(collection);
        
            Consistency.CheckForUnique(
                hashTable,
                dataTable,
                set.Count,
                resizeHandler,
                DogPlaceColorProjector.GetHashTupleComputer());
        }
        
        while (referenceSet.Count > 0)
        {
            var tuple = referenceSet.First();
            referenceSet.Remove(tuple);
            Assert.IsTrue(set.Remove(tuple));
            
            resizeHandler = resizeHandlerGetter(collection);
            
            var hashTable = hashTableGetter(resizeHandler);
            var dataTable = dataTableGetter(collection);
        
            Consistency.CheckForUnique(
                hashTable,
                dataTable,
                set.Count,
                resizeHandler,
                DogPlaceColorProjector.GetHashTupleComputer());
        }
        Assert.That(set, Is.Empty);
    }


    [Test]
    public void ClearDogPlaceColorTupleCollectionRandomlyTest()
    {
        var (_, factory) = NaryCollectionCompilation<DogPlaceColor>.GenerateCollectionConstructor(_moduleBuilder);

        var collection = factory();

        var set = collection.AsSet();
        
        var random = new Random(4223023);
        var someColors = Enum.GetValues<KnownColor>().Take(10).Select(Color.FromKnownColor).ToArray();
        
        for(int i = 0; i < 1000; ++i)
        {
            Dog dog = Dogs.AllDogs[random.Next(Dogs.AllDogs.Count)];
            Color color = someColors[random.Next(someColors.Length)];
            DogPlaceColorTuple tuple = (dog, "France", color);
            set.Add(tuple);
        }
        
        var manipulator = FieldManipulator.ForRealTypeOf(collection);

        var resizeHandlerGetter = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry>>(
            "_compositeHandler");

        var resizeHandler = resizeHandlerGetter(collection);
        
        var handlerManipulator = FieldManipulator.ForRealTypeOf(resizeHandler);
        
        var hashTableGetter = handlerManipulator.CreateGetter<HashEntry[]>("_hashTable");
        var dataTableGetter = manipulator
            .CreateGetter<DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[]>("_dataTable");
        
        resizeHandler = resizeHandlerGetter(collection);
            
        var hashTable = hashTableGetter(resizeHandler);
        var dataTable = dataTableGetter(collection);
        
        Consistency.CheckForUnique(
            hashTable,
            dataTable,
            set.Count,
            resizeHandler,
            DogPlaceColorProjector.GetHashTupleComputer());
        
        set.Clear();
        
        resizeHandler = resizeHandlerGetter(collection);
        hashTable = hashTableGetter(resizeHandler);
        dataTable = dataTableGetter(collection);
        
        Assert.That(set, Is.Empty);
        
        Consistency.CheckForUnique(
            hashTable,
            dataTable,
            set.Count,
            resizeHandler,
            DogPlaceColorProjector.GetHashTupleComputer());

        foreach (var hashEntry in hashTable)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
        foreach (var dataEntry in dataTable)
            Assert.That(dataEntry.DataTuple.Dog, Is.Null);
    }
}