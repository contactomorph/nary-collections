using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NaryCollections.Tools;

public readonly struct ValueTupleMapping : IReadOnlyList<(byte Index, FieldInfo Field)>, IEquatable<ValueTupleMapping>
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
    private readonly (byte Index, FieldInfo Field)[]? _indexedFields;
    
    public ValueTupleType InputType { get; }
    public ValueTupleType OutputType { get; }
    public int Count => _indexedFields?.Length ?? 0;

    private ValueTupleMapping(
        ValueTupleType inputType,
        ValueTupleType outputType,
        LambdaExpression expression,
        (byte, FieldInfo)[]? indexedFields)
    {
        InputType = inputType;
        OutputType = outputType;
        _expression = expression;
        _indexedFields = indexedFields;
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
            List<(byte Index, FieldInfo Field)> indexedFields = new();
            foreach (var position in outputPositions)
            {
                if (inputType.Count <= position)
                    throw new ArgumentOutOfRangeException(nameof(outputPositions));
                indexedFields.Add((position, inputType[position]));
            }

            var outputTypes = indexedFields.Select(f => f.Field.FieldType).ToArray();
            var outputType = ValueTupleType.FromComponents(outputTypes);

            var parameter = Expression.Parameter(inputType, "tuple");
            var fieldAccesses = indexedFields.Select(f => Expression.MakeMemberAccess(parameter, f.Field)).ToArray();
            var ctorCall = Expression.New(outputType.GetConstructor(), fieldAccesses);
            var expression = Expression.Lambda(ctorCall, parameter);
            
            return new(inputType, outputType, expression, indexedFields.ToArray());   
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
        if (_indexedFields is null) return other._indexedFields is null;
        if (other._indexedFields is null) return false;
        return _indexedFields.Length == other._indexedFields.Length
               && InputType == other.InputType
               && _indexedFields.SequenceEqual(other._indexedFields);
    }

    public IEnumerator<(byte Index, FieldInfo Field)> GetEnumerator()
    {
        return ((IEnumerable<(byte, FieldInfo)>?)_indexedFields ?? []).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public (byte Index, FieldInfo Field) this[int index]
    {
        get
        {
            if (_indexedFields is null || _indexedFields.Length <= index)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _indexedFields[index];
        }
    }

    public override bool Equals(object? obj) => obj is ValueTupleMapping other && Equals(other);

    public override int GetHashCode()
    {
        int hc = InputType.GetHashCode();
        return _indexedFields is null ? hc : _indexedFields.Aggregate(hc, HashCode.Combine);
    }

    public override string ToString()
    {
        if (InputType == ValueTupleType.Empty) return "(): [] ==> (): []";
        var body = _indexedFields?.Select(f => $"t{f.Index}") ?? [];
        return new StringBuilder("t: ")
            .Append(InputType)
            .Append(" => (")
            .AppendJoin(", ", body)
            .Append("): ")
            .Append(OutputType)
            .ToString();
    }
}