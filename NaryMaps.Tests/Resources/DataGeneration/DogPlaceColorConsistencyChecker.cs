using System.Drawing;
using NaryMaps.Primitives;
using NaryMaps.Tests.Resources.Tools;
using NaryMaps.Tests.Resources.Types;

namespace NaryMaps.Tests.Resources.DataGeneration;

using DogPlaceColorEntry = DataEntry<
    (Dog Dog, string Place, Color Color),
    (uint, uint, uint),
    (int, MultiIndex, MultiIndex, int, MultiIndex, MultiIndex)>;

public sealed class DogPlaceColorConsistencyChecker
{
    private readonly Func<INaryMap<DogPlaceColor>, DogPlaceColorEntry[]> _dataTableGetter;
    private readonly Func<INaryMap<DogPlaceColor>, int> _countGetter;
    private readonly Func<INaryMap<DogPlaceColor>, IResizeHandler<DogPlaceColorEntry, int>> _g0;
    private readonly Func<INaryMap<DogPlaceColor>, IResizeHandler<DogPlaceColorEntry, MultiIndex>> _g1;
    private readonly Func<INaryMap<DogPlaceColor>, IResizeHandler<DogPlaceColorEntry, MultiIndex>> _g2;
    private readonly Func<INaryMap<DogPlaceColor>, IResizeHandler<DogPlaceColorEntry, int>> _g3;
    private readonly Func<INaryMap<DogPlaceColor>, IResizeHandler<DogPlaceColorEntry, MultiIndex>> _g4;
    private readonly Func<INaryMap<DogPlaceColor>, IResizeHandler<DogPlaceColorEntry, MultiIndex>> _g5;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, int>, HashEntry[]> _htg0;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, HashEntry[]> _htg1;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, HashEntry[]> _htg2;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, int>, HashEntry[]> _htg3;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, HashEntry[]> _htg4;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, HashEntry[]> _htg5;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, int> _count1Getter;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, int> _count2Getter;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, int> _count4Getter;
    private readonly Func<IResizeHandler<DogPlaceColorEntry, MultiIndex>, int> _count5Getter;
    private readonly Func<(Dog Dog, string Place, Color Color), (uint, uint, uint)> _computer;

    public DogPlaceColorConsistencyChecker(INaryMap<DogPlaceColor> map)
    {
        var manipulator = FieldManipulator.ForRealTypeOf(map);
        
        _dataTableGetter = manipulator
            .CreateGetter<DogPlaceColorEntry[]>("_dataTable");
        _countGetter = manipulator.CreateGetter<int>("_count");
        
        _g0 = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry, int>>("_compositeHandler");
        _g1 = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry, MultiIndex>>("_compositeHandler_1");
        _g2 = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry, MultiIndex>>("_compositeHandler_2");
        _g3 = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry, int>>("_compositeHandler_3");
        _g4 = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry, MultiIndex>>("_compositeHandler_4");
        _g5 = manipulator.CreateGetter<IResizeHandler<DogPlaceColorEntry, MultiIndex>>("_compositeHandler_5");
        
        var manipulator0 = FieldManipulator.ForRealTypeOf(_g0(map));
        var manipulator1 = FieldManipulator.ForRealTypeOf(_g1(map));
        var manipulator2 = FieldManipulator.ForRealTypeOf(_g2(map));
        var manipulator3 = FieldManipulator.ForRealTypeOf(_g3(map));
        var manipulator4 = FieldManipulator.ForRealTypeOf(_g4(map));
        var manipulator5 = FieldManipulator.ForRealTypeOf(_g5(map));
        
        _htg0 = manipulator0.CreateGetter<HashEntry[]>("_hashTable");
        _htg1 = manipulator1.CreateGetter<HashEntry[]>("_hashTable");
        _htg2 = manipulator2.CreateGetter<HashEntry[]>("_hashTable");
        _htg3 = manipulator3.CreateGetter<HashEntry[]>("_hashTable");
        _htg4 = manipulator4.CreateGetter<HashEntry[]>("_hashTable");
        _htg5 = manipulator5.CreateGetter<HashEntry[]>("_hashTable");
        
        _count1Getter = manipulator1.CreateGetter<int>("_count");
        _count2Getter = manipulator2.CreateGetter<int>("_count");
        _count4Getter = manipulator4.CreateGetter<int>("_count");
        _count5Getter = manipulator5.CreateGetter<int>("_count");
        
        _computer = DogPlaceColorProjector.GetHashTupleComputer();
    }

    public void CheckConsistency(INaryMap<DogPlaceColor> map)
    {
        int count = _countGetter(map);
        var dataTable = _dataTableGetter(map);
        var handler0 = _g0(map);
        var handler1 = _g1(map);
        var handler2 = _g2(map);
        var handler3 = _g3(map);
        var handler4 = _g4(map);
        var handler5 = _g5(map);
        var hashTable0 = _htg0(handler0);
        var hashTable1 = _htg1(handler1);
        var hashTable2 = _htg2(handler2);
        var hashTable3 = _htg3(handler3);
        var hashTable4 = _htg4(handler4);
        var hashTable5 = _htg5(handler5);
        
        Consistency.CheckForUnique(hashTable0, dataTable, count, handler0, _computer);
        Consistency.CheckForNonUnique(hashTable1, dataTable, count, handler1, _computer);
        Consistency.CheckForNonUnique(hashTable2, dataTable, count, handler2, _computer);
        Consistency.CheckForUnique(hashTable3, dataTable, count, handler3, _computer);
        Consistency.CheckForNonUnique(hashTable4, dataTable, count, handler4, _computer);
        Consistency.CheckForNonUnique(hashTable5, dataTable, count, handler5, _computer);
    }

    public void CheckConsistencyAndCounts(
        INaryMap<DogPlaceColor> map,
        int expectedCount,
        int expectedCount1,
        int expectedCount2,
        int expectedCount4,
        int expectedCount5)
    {
        CheckConsistency(map);
        Assert.That(_countGetter(map), Is.EqualTo(expectedCount));
        Assert.That(_count1Getter(_g1(map)), Is.EqualTo(expectedCount1));
        Assert.That(_count2Getter(_g2(map)), Is.EqualTo(expectedCount2));
        Assert.That(_count4Getter(_g4(map)), Is.EqualTo(expectedCount4));
        Assert.That(_count5Getter(_g5(map)), Is.EqualTo(expectedCount5));
    }

    public void CheckIsEmpty(INaryMap<DogPlaceColor> map)
    {
        
        var dataTable = _dataTableGetter(map);
        var handler0 = _g0(map);
        var handler1 = _g1(map);
        var handler2 = _g2(map);
        var handler3 = _g3(map);
        var handler4 = _g4(map);
        var handler5 = _g5(map);
        var hashTable0 = _htg0(handler0);
        var hashTable1 = _htg1(handler1);
        var hashTable2 = _htg2(handler2);
        var hashTable3 = _htg3(handler3);
        var hashTable4 = _htg4(handler4);
        var hashTable5 = _htg5(handler5);
        
        Assert.That(_countGetter(map), Is.EqualTo(0));
        Assert.That(_count1Getter(handler1), Is.EqualTo(0));
        Assert.That(_count2Getter(handler2), Is.EqualTo(0));
        Assert.That(_count4Getter(handler4), Is.EqualTo(0));
        Assert.That(_count5Getter(handler5), Is.EqualTo(0));
        
        foreach (var dataEntry in dataTable)
            Assert.That(dataEntry.DataTuple.Dog, Is.Null);
        foreach (var hashEntry in hashTable0)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
        foreach (var hashEntry in hashTable1)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
        foreach (var hashEntry in hashTable2)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
        foreach (var hashEntry in hashTable3)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
        foreach (var hashEntry in hashTable4)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
        foreach (var hashEntry in hashTable5)
            Assert.That(hashEntry, Is.EqualTo(default(HashEntry)));
    }
}