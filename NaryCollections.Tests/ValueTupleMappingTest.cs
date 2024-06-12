using System.Linq.Expressions;
using NaryCollections.Tools;

namespace NaryCollections.Tests;

public class ValueTupleMappingTest
{
    [Test]
    public void EmptyToEmptyTest()
    {
        Assert.That(ValueTupleMapping.EmptyTupleToEmptyTuple.MappingFields, Is.Empty);
        Assert.That(ValueTupleMapping.EmptyTupleToEmptyTuple.InputType, Is.EqualTo(ValueTupleType.Empty));
        Assert.That(ValueTupleMapping.EmptyTupleToEmptyTuple.OutputType, Is.EqualTo(ValueTupleType.Empty));
        
        LambdaExpression expr = ValueTupleMapping.EmptyTupleToEmptyTuple;
        Assert.That(expr.Parameters.Single().Type, Is.EqualTo(typeof(ValueTuple)));
        Assert.That(expr.ReturnType, Is.EqualTo(typeof(ValueTuple)));
        Assert.That(
            ValueTupleMapping.EmptyTupleToEmptyTuple.Compile().DynamicInvoke(new ValueTuple()),
            Is.EqualTo(new ValueTuple()));
    }
    
    [Test]
    public void EmptyOutputTest()
    {
        var inputType = ValueTupleType.FromComponents(typeof(uint), typeof(Guid), typeof(Uri));
        var mapping = ValueTupleMapping.From(inputType);
        
        Assert.That(mapping.MappingFields, Is.Empty);
        Assert.That(mapping.InputType, Is.EqualTo(inputType));
        Assert.That(mapping.OutputType, Is.EqualTo(ValueTupleType.Empty));
        
        LambdaExpression expr = mapping;
        Assert.That(expr.Parameters.Single().Type, Is.EqualTo((Type)inputType));
        Assert.That(expr.ReturnType, Is.EqualTo(typeof(ValueTuple)));
        Assert.That(
            mapping.Compile().DynamicInvoke((23U, Guid.NewGuid(), new Uri("http://hello.com"))),
            Is.EqualTo(new ValueTuple()));
    }
    
    [Test]
    public void ElaboratedMappingTest()
    {
        var inputType = ValueTupleType.FromComponents(typeof(uint), typeof(Guid), typeof(Uri));
        var mapping = ValueTupleMapping.From(inputType, [2, 2, 1, 0]);

        var expectedValueType = ValueTupleType.FromComponents(typeof(Uri), typeof(Uri), typeof(Guid), typeof(uint));
        Assert.That(
            mapping.MappingFields.Select(f => f.ToString()),
            Is.EqualTo(new [] { "System.Uri Item3", "System.Uri Item3", "System.Guid Item2", "UInt32 Item1" }));
        Assert.That(mapping.InputType, Is.EqualTo(inputType));
        Assert.That(mapping.OutputType, Is.EqualTo(expectedValueType));
        
        LambdaExpression expr = mapping;
        Assert.That(expr.Parameters.Single().Type, Is.EqualTo((Type)inputType));
        Assert.That(expr.ReturnType, Is.EqualTo((Type)expectedValueType));

        var input = (23U, Guid.NewGuid(), new Uri("http://hello.com"));
        Assert.That(
            mapping.Compile().DynamicInvoke(input),
            Is.EqualTo((input.Item3, input.Item3, input.Item2, input.Item1)));
    }
}