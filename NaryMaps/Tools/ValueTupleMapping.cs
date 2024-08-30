using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NaryMaps.Tools;

using Correspondence = (Type Type, byte OutputIndex, FieldInfo OutputField, byte InputIndex, FieldInfo InputField);

public readonly struct ValueTupleMapping : IReadOnlyList<Correspondence>, IEquatable<ValueTupleMapping>
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
    private readonly Correspondence[]? _correspondences;
    
    public ValueTupleType InputType { get; }
    public ValueTupleType OutputType { get; }
    public int Count => _correspondences?.Length ?? 0;

    private ValueTupleMapping(
        ValueTupleType inputType,
        ValueTupleType outputType,
        LambdaExpression expression,
        Correspondence[]? correspondences)
    {
        InputType = inputType;
        OutputType = outputType;
        _expression = expression;
        _correspondences = correspondences;
    }

    public static ValueTupleMapping From(ValueTupleType inputType, params byte[] inputPositions)
    {
        if (inputPositions == null) throw new ArgumentNullException(nameof(inputPositions));
        if (inputPositions.Length == 0)
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
            List<FieldInfo> inputComponentFields = new();
            foreach (var position in inputPositions)
            {
                if (inputType.Count <= position)
                    throw new ArgumentOutOfRangeException(nameof(inputPositions));
                inputComponentFields.Add(inputType[position]);
            }
            var outputTypes = inputComponentFields.Select(f => f.FieldType).ToArray();
            var outputType = ValueTupleType.FromComponents(outputTypes);

            List<Correspondence> correspondences = new();
            byte outputPosition = 0;
            foreach (var position in inputPositions)
            {
                Correspondence correspondence = (
                    Type: outputTypes[outputPosition],
                    OutputIndex: outputPosition,
                    OutputField: outputType[outputPosition],
                    InputIndex: position,
                    InputField: inputType[position]);
                correspondences.Add(correspondence);
                ++outputPosition;
            }

            var parameter = Expression.Parameter(inputType, "tuple");
            var fieldAccesses = correspondences
                .Select(f => Expression.MakeMemberAccess(parameter, f.InputField))
                .ToArray();
            var ctorCall = Expression.New(outputType.GetConstructor(), fieldAccesses);
            var expression = Expression.Lambda(ctorCall, parameter);
            
            return new(inputType, outputType, expression, correspondences.ToArray());   
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
        if (_correspondences is null) return other._correspondences is null;
        if (other._correspondences is null) return false;
        return _correspondences.Length == other._correspondences.Length
               && InputType == other.InputType
               && _correspondences.SequenceEqual(other._correspondences);
    }

    public IEnumerator<Correspondence> GetEnumerator()
    {
        return ((IEnumerable<Correspondence>?)_correspondences ?? []).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public Correspondence this[int index]
    {
        get
        {
            if (_correspondences is null || _correspondences.Length <= index)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _correspondences[index];
        }
    }

    public override bool Equals(object? obj) => obj is ValueTupleMapping other && Equals(other);

    public override int GetHashCode()
    {
        int hc = InputType.GetHashCode();
        return _correspondences is null ? hc : _correspondences.Aggregate(hc, HashCode.Combine);
    }

    public override string ToString()
    {
        if (InputType == ValueTupleType.Empty) return "(): [] ==> (): []";
        var body = _correspondences?.Select(f => $"t{f.InputIndex}") ?? [];
        return new StringBuilder("t: ")
            .Append(InputType)
            .Append(" => (")
            .AppendJoin(", ", body)
            .Append("): ")
            .Append(OutputType)
            .ToString();
    }
}