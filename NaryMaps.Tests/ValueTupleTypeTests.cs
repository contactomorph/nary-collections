using System.Linq.Expressions;
using System.Reflection;
using NaryMaps.Tests.Resources.Types;
using NaryMaps.Tools;

namespace NaryMaps.Tests;

public class ValueTupleTypeTests
{
    [Test]
    public void EmptyTest()
    {
        Assert.That(ValueTupleType.From(typeof(ValueTuple)).GetValueOrDefault(), Is.EqualTo(ValueTupleType.Empty));
        Assert.That(ValueTupleType.FromComponents(), Is.EqualTo(ValueTupleType.Empty));
        Assert.That(ValueTupleType.FromRepeatedComponent(typeof(Dog), 0), Is.EqualTo(ValueTupleType.Empty));
        Assert.That(ValueTupleType.FromRepeatedComponent<string>(0), Is.EqualTo(ValueTupleType.Empty));
        
        Assert.That((Type)ValueTupleType.Empty, Is.EqualTo(typeof(ValueTuple)));
            
        Assert.That(ValueTupleType.Empty.Select(f => (f.Name, f.FieldType)), Is.Empty);
    }

    [Test]
    public void CompositionTest()
    {
        {
            var vtt = ValueTupleType.From(typeof((string, ulong))).GetValueOrDefault();
        
            Assert.That((Type)vtt, Is.EqualTo(typeof((string, ulong))));
            
            Assert.That(
                vtt.Select(f => (f.Name, f.FieldType)),
                Is.EqualTo(new[] { ("Item1", typeof(string)), ("Item2", typeof(ulong)) }));
        }
        {
            var vtt = ValueTupleType.From(typeof((Dog, string, ulong, Guid))).GetValueOrDefault();
        
            Assert.That((Type)vtt, Is.EqualTo(typeof((Dog, string, ulong, Guid))));
            
            Assert.That(
                vtt.Select(f => (f.Name, f.FieldType)),
                Is.EqualTo(new[]
                {
                    ("Item1", typeof(Dog)),
                    ("Item2", typeof(string)),
                    ("Item3", typeof(ulong)),
                    ("Item4", typeof(Guid))
                }));
        }
        {
            var vtt = ValueTupleType.FromRepeatedComponent<uint>(3);
        
            Assert.That((Type)vtt, Is.EqualTo(typeof((uint, uint, uint))));
            
            Assert.That(
                vtt.Select(f => (f.Name, f.FieldType)),
                Is.EqualTo(new[] { ("Item1", typeof(uint)), ("Item2", typeof(uint)), ("Item3", typeof(uint)) }));
        }
    }

    [Test]
    public void ConstructorTest()
    {
        {
            Type[] componentTypes = [typeof(string), typeof(ulong)];
            var ctor = ValueTupleType.FromComponents(componentTypes).GetConstructor();
        
            Assert.That(ctor.DeclaringType!.GenericTypeArguments, Is.EqualTo(componentTypes));
        
            var f = CreateLambda<Func<string, ulong, (string, ulong)>>(ctor);
        
            Assert.That(f("Youpi", 17L), Is.EqualTo(("Youpi", 17L)));
        }
        
        {
            Type[] componentTypes = [typeof(Dog), typeof(string), typeof(ulong), typeof(Guid)];
            var ctor = ValueTupleType.FromComponents(componentTypes).GetConstructor();
        
            Assert.That(ctor.DeclaringType!.GenericTypeArguments, Is.EqualTo(componentTypes));
        
            var f = CreateLambda<Func<Dog, string, ulong, Guid, (Dog, string, ulong, Guid)>>(ctor);
        
            var dog = new Dog("Zoubi", "Régis");
            var id = Guid.NewGuid();
            Assert.That(f(dog, "bonjour", 4L, id), Is.EqualTo((dog, "bonjour", 4L, id)));
        }
    }

    private TFunc CreateLambda<TFunc>(ConstructorInfo ctor)
    {
        var parameters = ctor
            .GetParameters()
            .Select((p, i) => Expression.Parameter(p.ParameterType, $"p{i}"))
            .ToArray();
        return Expression.Lambda<TFunc>(Expression.New(ctor, parameters), parameters).Compile();
    }
}