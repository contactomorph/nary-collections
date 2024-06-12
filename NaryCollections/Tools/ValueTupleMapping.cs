using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NaryCollections.Tools;

public readonly struct ValueTupleMapping : IEquatable<ValueTupleMapping>
{
    private static readonly LambdaExpression EmptyLambdaExpression;

    static ValueTupleMapping()
    {
        EmptyLambdaExpression = Expression.Lambda(
            Expression.Constant(new ValueTuple()),
            Expression.Parameter(typeof(ValueTuple), "tuple"));
    }
    
    public static readonly ValueTupleMapping EmptyTupleToEmptyTuple = default;

    private readonly LambdaExpression? _expression;
    private readonly FieldInfo[]? _mappingFields;
    
    public ValueTupleType InputType { get; }
    public ValueTupleType OutputType { get; }

    public IReadOnlyList<FieldInfo> MappingFields => _mappingFields ?? [];

    private ValueTupleMapping(
        ValueTupleType inputType,
        ValueTupleType outputType,
        LambdaExpression expression,
        FieldInfo[]? mappingFields)
    {
        InputType = inputType;
        OutputType = outputType;
        _expression = expression;
        _mappingFields = mappingFields;
    }

    public static ValueTupleMapping From(ValueTupleType inputType, params byte[] outputPositions)
    {
        if (outputPositions == null) throw new ArgumentNullException(nameof(outputPositions));
        if (outputPositions.Length == 0)
        {
            if (inputType == ValueTupleType.Empty)
                return default;
            var expression = Expression.Lambda(
                Expression.Constant(new ValueTuple()),
                Expression.Parameter(inputType, "tuple"));
            return new(inputType, ValueTupleType.Empty, expression, null);
        }
        else
        {
            List<FieldInfo> mappingFields = new();
            foreach (var position in outputPositions)
            {
                if (inputType.Count <= position)
                    throw new ArgumentOutOfRangeException(nameof(outputPositions));
                mappingFields.Add(inputType[position]);
            }

            var outputType = ValueTupleType.FromComponents(mappingFields.Select(f => f.FieldType).ToArray());

            var parameter = Expression.Parameter(inputType, "tuple");
            var fieldAccesses = mappingFields.Select(f => Expression.MakeMemberAccess(parameter, f)).ToArray();
            var ctorCall = Expression.New(outputType.GetConstructor(), fieldAccesses);
            var expression = Expression.Lambda(ctorCall, parameter);
            
            return new(inputType, outputType, expression, mappingFields.ToArray());   
        }
    }

    public static implicit operator LambdaExpression(ValueTupleMapping mapping)
    {
        return mapping._expression ?? EmptyLambdaExpression;
    }
    
    public static bool operator ==(ValueTupleMapping a, ValueTupleMapping b) => a.Equals(b);

    public static bool operator !=(ValueTupleMapping a, ValueTupleMapping b) => !a.Equals(b);

    public Delegate Compile() => (_expression ?? EmptyLambdaExpression).Compile();

    public bool Equals(ValueTupleMapping other)
    {
        if (_mappingFields is null) return other._mappingFields is null;
        if (other._mappingFields is null) return false;
        return _mappingFields.Length == other._mappingFields.Length
               && InputType == other.InputType
               && _mappingFields.SequenceEqual(other._mappingFields);
    }

    public override bool Equals(object? obj) => obj is ValueTupleMapping other && Equals(other);

    public override int GetHashCode()
    {
        int hc = InputType.GetHashCode();
        return _mappingFields is null ? hc : _mappingFields.Aggregate(hc, HashCode.Combine);
    }

    public override string ToString()
    {
        if (InputType == ValueTupleType.Empty) return "(): [] ==> (): []";
        var body = _mappingFields?.Select(f => $"t.{f.Name}") ?? [];
        return new StringBuilder("t: ")
            .Append(InputType)
            .Append(" => (")
            .AppendJoin(", ", body)
            .Append("): ")
            .Append(OutputType)
            .ToString();
    }
}