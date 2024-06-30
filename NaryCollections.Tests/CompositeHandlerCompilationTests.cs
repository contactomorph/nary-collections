using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tests.Resources.Types;

namespace NaryCollections.Tests;

using DogPlaceColorTuple = (Dog Dog, string Place, Color Color);
using HashTuple = (uint, uint, uint);
using BackIndexTuple = (int, int, int, int);
using ComparerTuple = (IEqualityComparer<Dog>, IEqualityComparer<string>, IEqualityComparer<Color>);

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
            [0, 1, 2],
            0,
            4);
        
        var del = Expression.Lambda(Expression.New(ctor, Expression.Constant(true))).Compile();

        var untypedHandler =  del.DynamicInvoke();
        Assert.That(
            untypedHandler,
            Is.Not.Null
                .And.InstanceOf<ICompositeHandler<DogPlaceColorTuple, HashTuple, BackIndexTuple, ComparerTuple, DogPlaceColorTuple>>());
    }
}