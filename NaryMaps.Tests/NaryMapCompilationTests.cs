using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryMaps.Implementation;
using NaryMaps.Primitives;
using NaryMaps.Tests.Resources.Data;
using NaryMaps.Tests.Resources.DataGeneration;
using NaryMaps.Tests.Resources.Tools;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using HashTuple = (uint, uint, uint);
using IndexTuple = (int, MultiIndex, MultiIndex, int, MultiIndex, MultiIndex);
using DogPlaceColorEntry = DataEntry<
    (Dog Dog, string Place, Color Color),
    (uint, uint, uint),
    (int, MultiIndex, MultiIndex, int, MultiIndex, MultiIndex)>;
using NakedIndexTuple = ValueTuple<int>;
using NakedDogPlaceColorEntry = DataEntry<
    (Dog Dog, string Place, Color Color),
    (uint, uint, uint),
    ValueTuple<int>>;

delegate bool FindInOtherComposites(DogPlaceColorTuple dataTuple, HashTuple hashTuple, out SearchResult[] otherResults);

public class NaryMapCompilationTests
{
    private ModuleBuilder _moduleBuilder;
    
    [SetUp]
    public void Setup()
    {
        AssemblyName assembly = new AssemblyName { Name = "nary_map_test_2_.dll" };
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule("nary_map_module_test");
    }
    
    [Test]
    public void CompileNakedDogPlaceColorTupleMapTest()
    {
        var (_, factory) = NaryMapCompilation<NakedDogPlaceColor>.GenerateMapConstructor(_moduleBuilder);

        var map = factory();
        
        var set = map.AsSet();
        
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
    public void FillNakedDogPlaceColorTupleMapRandomlyTest()
    {
        var (_, factory) = NaryMapCompilation<NakedDogPlaceColor>.GenerateMapConstructor(_moduleBuilder);

        var map = factory();
        
        var manipulator = FieldManipulator.ForRealTypeOf(map);

        var resizeHandlerGetter = manipulator.CreateGetter<IResizeHandler<NakedDogPlaceColorEntry, int>>(
            "_compositeHandler");

        var resizeHandler = resizeHandlerGetter(map);
        
        var handlerManipulator = FieldManipulator.ForRealTypeOf(resizeHandler);
        
        var hashTableGetter = handlerManipulator.CreateGetter<HashEntry[]>("_hashTable");
        var dataTableGetter = manipulator
            .CreateGetter<DataEntry<DogPlaceColorTuple, HashTuple, NakedIndexTuple>[]>("_dataTable");

        var set = map.AsSet();
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
            
            resizeHandler = resizeHandlerGetter(map);
            
            var hashTable = hashTableGetter(resizeHandler);
            var dataTable = dataTableGetter(map);
        
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
            
            resizeHandler = resizeHandlerGetter(map);
            
            var hashTable = hashTableGetter(resizeHandler);
            var dataTable = dataTableGetter(map);
        
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
    public void ClearDogPlaceColorTupleMapRandomlyTest()
    {
        var (_, factory) = NaryMapCompilation<DogPlaceColor>.GenerateMapConstructor(_moduleBuilder);

        var map = factory();

        var set = map.AsSet();
        
        var random = new Random(4223023);
        var someColors = Enum.GetValues<KnownColor>().Take(10).Select(Color.FromKnownColor).ToArray();
        
        for(int i = 0; i < 1000; ++i)
        {
            Dog dog = Dogs.AllDogs[random.Next(Dogs.AllDogs.Count)];
            Color color = someColors[random.Next(someColors.Length)];
            DogPlaceColorTuple tuple = (dog, "France", color);
            set.Add(tuple);
        }
        
        var manipulator = FieldManipulator.ForRealTypeOf(map);

        var resizeHandlerGetter = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry, int>>(
            "_compositeHandler");

        var resizeHandler = resizeHandlerGetter(map);
        
        var handlerManipulator = FieldManipulator.ForRealTypeOf(resizeHandler);
        
        var hashTableGetter = handlerManipulator.CreateGetter<HashEntry[]>("_hashTable");
        var dataTableGetter = manipulator
            .CreateGetter<DataEntry<DogPlaceColorTuple, HashTuple, IndexTuple>[]>("_dataTable");
        
        resizeHandler = resizeHandlerGetter(map);
            
        var hashTable = hashTableGetter(resizeHandler);
        var dataTable = dataTableGetter(map);
        
        Consistency.CheckForUnique(
            hashTable,
            dataTable,
            set.Count,
            resizeHandler,
            DogPlaceColorProjector.GetHashTupleComputer());
        
        set.Clear();
        
        resizeHandler = resizeHandlerGetter(map);
        hashTable = hashTableGetter(resizeHandler);
        dataTable = dataTableGetter(map);
        
        Assert.That(set, Is.Empty);
        
        AssertCounts(
            map,
            expectedCount0: 0,
            expectedCount1: 0,
            expectedCount2: 0,
            expectedCount4: 0,
            expectedCount5: 0);

        foreach (var hashEntry in hashTable)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
        foreach (var dataEntry in dataTable)
            Assert.That(dataEntry.DataTuple.Dog, Is.Null);
    }
    
    [Test]
    public void InspectDogPlaceColorTupleMapTest()
    {
        var (_, factory) = NaryMapCompilation<DogPlaceColor>.GenerateMapConstructor(_moduleBuilder);

        var map = factory();
        
        var manipulator = FieldManipulator.ForRealTypeOf(map);

        manipulator.GetFieldValue(map, "_compositeHandler", out IResizeHandler<DogPlaceColorEntry, int> h0);
        manipulator.GetFieldValue(map, "_compositeHandler_1", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h1);
        manipulator.GetFieldValue(map, "_compositeHandler_2", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h2);
        manipulator.GetFieldValue(map, "_compositeHandler_3", out IResizeHandler<DogPlaceColorEntry, int> h3);
        manipulator.GetFieldValue(map, "_compositeHandler_4", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h4);
        manipulator.GetFieldValue(map, "_compositeHandler_5", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h5);
        
        var manipulatorForH0 = FieldManipulator.ForRealTypeOf(h0);
        manipulatorForH0.GetFieldValue(h0, "_hashTable", out HashEntry[] hashTable0);
        
        Assert.That(hashTable0.Select(h => h.DriftPlusOne), Is.All.Zero);
        
        var manipulatorForH1 = FieldManipulator.ForRealTypeOf(h1);
        manipulatorForH1.GetFieldValue(h1, "_hashTable", out HashEntry[] hashTable1);
        
        Assert.That(hashTable1.Select(h => h.DriftPlusOne), Is.All.Zero);
        
        var manipulatorForH2 = FieldManipulator.ForRealTypeOf(h2);
        manipulatorForH2.GetFieldValue(h2, "_hashTable", out HashEntry[] hashTable2);
        
        Assert.That(hashTable2.Select(h => h.DriftPlusOne), Is.All.Zero);
        
        var manipulatorForH3 = FieldManipulator.ForRealTypeOf(h3);
        manipulatorForH3.GetFieldValue(h3, "_hashTable", out HashEntry[] hashTable3);
        
        Assert.That(hashTable3.Select(h => h.DriftPlusOne), Is.All.Zero);
        
        var manipulatorForH4 = FieldManipulator.ForRealTypeOf(h4);
        manipulatorForH4.GetFieldValue(h4, "_hashTable", out HashEntry[] hashTable4);
        
        Assert.That(hashTable4.Select(h => h.DriftPlusOne), Is.All.Zero);
        
        var manipulatorForH5 = FieldManipulator.ForRealTypeOf(h5);
        manipulatorForH5.GetFieldValue(h5, "_hashTable", out HashEntry[] hashTable5);
        
        Assert.That(hashTable5.Select(h => h.DriftPlusOne), Is.All.Zero);
        
        Fill(hashTable1, 10);
        Fill(hashTable2, 20);
        Fill(hashTable3, 30);
        Fill(hashTable4, 40);
        Fill(hashTable5, 50);
        
        Assert.That(hashTable1.Select(h => h.ForwardIndex), Is.All.EqualTo(10));
        Assert.That(hashTable2.Select(h => h.ForwardIndex), Is.All.EqualTo(20));
        Assert.That(hashTable3.Select(h => h.ForwardIndex), Is.All.EqualTo(30));
        Assert.That(hashTable4.Select(h => h.ForwardIndex), Is.All.EqualTo(40));
        Assert.That(hashTable5.Select(h => h.ForwardIndex), Is.All.EqualTo(50));
    }

    private void Fill(HashEntry[] hashTable, int forwardIndex)
    {
        for (int i = 0; i < hashTable.Length; i++) hashTable[i].ForwardIndex = forwardIndex;
    }

    [Test]
    public void FindInOtherCompositeTest()
    {
        var (_, factory) = NaryMapCompilation<DogPlaceColor>.GenerateMapConstructor(_moduleBuilder);
        
        var map = factory();
        
        var naryCollectionBaseType = map.GetType().BaseType!;
        
        var method = naryCollectionBaseType.GetMethod(
            FakeNaryMap.FindInOtherCompositesMethodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!;

        var dataTupleParameter = Expression.Parameter(typeof(DogPlaceColorTuple), "dataTuple");
        var hashTupleParameter = Expression.Parameter(typeof(HashTuple), "hashTuple");
        var otherResultsParameter = Expression.Parameter(typeof(SearchResult[]).MakeByRefType(), "otherResults");
        var body = Expression.Call(
            Expression.Constant(map),
            method,
            dataTupleParameter,
            hashTupleParameter,
            otherResultsParameter);
        var find = Expression.Lambda<FindInOtherComposites>(
            body,
            dataTupleParameter,
            hashTupleParameter,
            otherResultsParameter).Compile();

        var alreadyInside = find.Invoke(
            (new Dog("Cannelle", "Jo"), "Nantes", Color.Lavender),
            (1, 2, 3),
            out var results);

        Assert.That(alreadyInside, Is.False);
        Assert.That(results, Has.Length.EqualTo(5));
        foreach (var searchResult in results)
        {
            Assert.That(searchResult.Case, Is.EqualTo(SearchCase.EmptyEntryFound));
            Assert.That(searchResult.DriftPlusOne, Is.EqualTo(HashEntry.Optimal));
        }
    }

    [Test]
    public void AddDogPlaceColorTupleMapTest()
    {
        var (_, factory) = NaryMapCompilation<DogPlaceColor>.GenerateMapConstructor(_moduleBuilder);
        
        var map = factory();

        var set = map.AsSet();

        var dog1 = Dogs.AllDogs[0];
        var dog2 = Dogs.AllDogs[1];
        var places = new[] { "Paris", "Lyon", "Nantes", "Bordeaux", "Toulouse", "Marseilles" };

        foreach (var place in places)
        {
            Assert.That(set.Add((dog1, place, Color.Aqua)), Is.True);
        }
        
        foreach (var place in places)
        {
            Assert.That(set.Add((dog2, place, Color.Aqua)), Is.False);
        }

        var otherPlaces = new[] { "Київ", "Харків", "Одеса", "Дніпро", "Донецьк", "Запоріжжя", "Львів", "Кривий Ріг", "Миколаїв" };
        var colors = new[] { Color.Bisque, Color.BlueViolet, Color.Brown, Color.BurlyWood, Color.CadetBlue };

        int i = 0;
        foreach (var place in otherPlaces)
        {
            var dog = Dogs.AllDogs[i % 4];
            var color = colors[i % 5];
            Assert.That(set.Add((dog, place, color)), Is.True);
            ++i;
        }

        AssertCounts(
            map,
            expectedCount0: 15,
            expectedCount1: 4,
            expectedCount2: 6,
            expectedCount4: 10,
            expectedCount5: 15);
    }

    [Test]
    public void AsDogPlaceColorComparerTest()
    {
        var (_, factory) = NaryMapCompilation<DogPlaceColor>.GenerateMapConstructor(_moduleBuilder);
        
        var comparer = (IEqualityComparer<DogPlaceColorTuple>)factory();
        
        var d0 = Dogs.KnownDogs[0];
        var d1 = Dogs.KnownDogs[1];

        DogPlaceColorTuple[] tuples = [
            (d0, "Paris", Color.Yellow),
            (d0, "Paris", Color.Red),
            (d0, "Lyon", Color.Yellow),
            (d0, "Lyon", Color.Red),
            (d1, "Paris", Color.Yellow),
            (d1, "Paris", Color.Red),
            (d1, "Lyon", Color.Yellow),
            (d1, "Lyon", Color.Red),
        ];

        var dogComparer = EqualityComparer<Dog>.Default;
        var stringComparer = EqualityComparer<string>.Default;
        var colorComparer = EqualityComparer<Color>.Default;

        for(int i = 0; i < tuples.Length; i++)
        {
            var hi = comparer.GetHashCode(tuples[i]);
            
            for (int j = 0; j < tuples.Length; j++)
            {
                var expected = i == j;
                var actual = comparer.Equals(tuples[i], tuples[j]);
                Assert.That(actual, Is.EqualTo(expected));
                var hj = comparer.GetHashCode(tuples[j]);
                Assert.That(hi == hj, Is.EqualTo(expected));
            }

            var hd = dogComparer.GetHashCode(tuples[i].Dog);
            var hp = stringComparer.GetHashCode(tuples[i].Place);
            var hc = colorComparer.GetHashCode(tuples[i].Color);
            
            Assert.That(hi, Is.EqualTo((hd, hp, hc).GetHashCode()));
        }
    }

    private static void AssertCounts(
        INaryMap<DogPlaceColor> map,
        int expectedCount0,
        int expectedCount1,
        int expectedCount2,
        int expectedCount4,
        int expectedCount5)
    {
        var manipulator = FieldManipulator.ForRealTypeOf(map);

        manipulator.GetFieldValue(map, "_dataTable", out DogPlaceColorEntry[] dataTable);
        manipulator.GetFieldValue(map, "_count", out int count);
        manipulator.GetFieldValue(map, "_compositeHandler", out IResizeHandler<DogPlaceColorEntry, int> h0);
        manipulator.GetFieldValue(map, "_compositeHandler_1", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h1);
        manipulator.GetFieldValue(map, "_compositeHandler_2", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h2);
        manipulator.GetFieldValue(map, "_compositeHandler_3", out IResizeHandler<DogPlaceColorEntry, int> h3);
        manipulator.GetFieldValue(map, "_compositeHandler_4", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h4);
        manipulator.GetFieldValue(map, "_compositeHandler_5", out IResizeHandler<DogPlaceColorEntry, MultiIndex> h5);

        Assert.That(count, Is.EqualTo(expectedCount0));

        var computer = DogPlaceColorProjector.GetHashTupleComputer();

        var manipulator0 = FieldManipulator.ForRealTypeOf(h0);
        manipulator0.GetFieldValue(h0, "_hashTable", out HashEntry[] hashTable0);

        Consistency.CheckForUnique(hashTable0, dataTable, count, h0, computer);

        var manipulator1 = FieldManipulator.ForRealTypeOf(h1);
        manipulator1.GetFieldValue(h1, "_hashTable", out HashEntry[] hashTable1);
        manipulator1.GetFieldValue(h1, "_count", out int count1);

        Assert.That(count1, Is.EqualTo(expectedCount1));

        Consistency.CheckForNonUnique(hashTable1, dataTable, count, h1, computer);

        var manipulator2 = FieldManipulator.ForRealTypeOf(h2);
        manipulator2.GetFieldValue(h2, "_hashTable", out HashEntry[] hashTable2);
        manipulator2.GetFieldValue(h2, "_count", out int count2);

        Assert.That(count2, Is.EqualTo(expectedCount2));

        Consistency.CheckForNonUnique(hashTable2, dataTable, count, h2, computer);

        var manipulator3 = FieldManipulator.ForRealTypeOf(h3);
        manipulator3.GetFieldValue(h3, "_hashTable", out HashEntry[] hashTable3);

        Consistency.CheckForUnique(hashTable3, dataTable, count, h3, computer);

        var manipulator4 = FieldManipulator.ForRealTypeOf(h4);
        manipulator4.GetFieldValue(h4, "_hashTable", out HashEntry[] hashTable4);
        manipulator4.GetFieldValue(h4, "_count", out int count4);

        Assert.That(count4, Is.EqualTo(expectedCount4));

        Consistency.CheckForNonUnique(hashTable4, dataTable, count, h4, computer);

        var manipulator5 = FieldManipulator.ForRealTypeOf(h5);
        manipulator5.GetFieldValue(h5, "_hashTable", out HashEntry[] hashTable5);
        manipulator5.GetFieldValue(h5, "_count", out int count5);

        Assert.That(count5, Is.EqualTo(expectedCount5));

        Consistency.CheckForNonUnique(hashTable5, dataTable, count, h5, computer);
    }
}