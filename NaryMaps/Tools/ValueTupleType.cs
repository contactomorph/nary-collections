using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NaryMaps.Tools;

public readonly struct ValueTupleType : IReadOnlyList<FieldInfo>, IEquatable<ValueTupleType>
{
    public static readonly ValueTupleType Empty = default;
    
    private readonly Type? _tupleType;
    private readonly FieldInfo[]? _componentFields;
    
    public int Count => _componentFields?.Length ?? 0;

    private ValueTupleType(Type tupleType, FieldInfo[] componentFields)
    {
        _tupleType = tupleType;
        _componentFields = componentFields;
    }

    public static implicit operator Type(ValueTupleType type) => type._tupleType ?? typeof(ValueTuple);
    
    public static bool operator ==(ValueTupleType a, ValueTupleType b) => a._tupleType == b._tupleType;

    public static bool operator !=(ValueTupleType a, ValueTupleType b) => a._tupleType != b._tupleType;
    
    public static bool operator ==(ValueTupleType a, Type b) => (Type)a == b;

    public static bool operator !=(ValueTupleType a, Type b) => (Type)a != b;


    public static ValueTupleType FromRepeatedComponent(Type componentType, int length)
    {
        if (componentType is null) throw new ArgumentNullException(nameof(componentType));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (length == 0) return default;
        if (7 < length) throw new NotSupportedException();
        var componentTypes = Enumerable.Repeat(componentType, length).ToArray();
        Type tupleType = GetTupleTypeDefinition(length).MakeGenericType(componentTypes);
        return CreateFromNonEmptyTupleType(tupleType);
    }

    public static ValueTupleType FromRepeatedComponent<T>(int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (length == 0) return default;
        Type tupleType = length switch
        {
            1 => typeof(ValueTuple<T>),
            2 => typeof((T, T)),
            3 => typeof((T, T, T)),
            4 => typeof((T, T, T, T)),
            5 => typeof((T, T, T, T, T)),
            6 => typeof((T, T, T, T, T, T)),
            7 => typeof((T, T, T, T, T, T, T)),
            _ => throw new NotSupportedException()
        };
        return CreateFromNonEmptyTupleType(tupleType);
    }

    public static ValueTupleType FromComponents(params Type[] componentTypes)
    {
        if (componentTypes is null) throw new ArgumentNullException(nameof(componentTypes));
        if (componentTypes.Length == 0) return default;
        if (7 < componentTypes.Length) throw new NotSupportedException();
        Type tupleType = GetTupleTypeDefinition(componentTypes.Length).MakeGenericType(componentTypes);
        return CreateFromNonEmptyTupleType(tupleType);
    }

    public static bool IsValueTupleType(Type type)
    {
        return type == typeof(ValueTuple) ||
            type is { IsValueType: true, IsConstructedGenericType: true } &&
            typeof(ITuple).IsAssignableFrom(type);
    }

    public static ValueTupleType? From(Type type)
    {
        if (!IsValueTupleType(type))
            return null;
        return type == typeof(ValueTuple) ? default : CreateFromNonEmptyTupleType(type);
    }

    public ConstructorInfo GetConstructor()
    {
        if (_tupleType is null)
            throw new InvalidOperationException("Non parametric ValueTuple type has no constructor");
        return _tupleType.GetConstructors().Single();
    }

    public IEnumerator<FieldInfo> GetEnumerator() => ((IEnumerable<FieldInfo>?)_componentFields ?? []).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public FieldInfo this[int index]
    {
        get
        {
            if (_componentFields is null || index < 0 || _componentFields.Length <= index)
                throw new IndexOutOfRangeException();
            return _componentFields[index];
        }
    }

    public bool Equals(ValueTupleType other) => _tupleType  == other._tupleType;

    public override bool Equals(object? obj) => obj is ValueTupleType other && Equals(other);

    public override int GetHashCode() => _tupleType is null ? 0 : _tupleType.GetHashCode();

    public override string ToString()
    {
        if (_componentFields is null) return "()";
        var names = _componentFields.Select(f => f.FieldType.Name);
        return new StringBuilder("[").AppendJoin(", ", names).Append(']').ToString();
    }

    private static ValueTupleType CreateFromNonEmptyTupleType(Type tupleType)
    {
        var componentFields = tupleType
            .GetFields()
            .Where(f => f.Name.StartsWith("Item"))
            .ToArray();
        return new ValueTupleType(tupleType, componentFields);
    }
    
    private static Type GetTupleTypeDefinition(int length)
    {
        return length switch
        {
            1 => typeof(ValueTuple<>),
            2 => typeof(ValueTuple<,>),
            3 => typeof(ValueTuple<,,>),
            4 => typeof(ValueTuple<,,,>),
            5 => typeof(ValueTuple<,,,,>),
            6 => typeof(ValueTuple<,,,,,>),
            7 => typeof(ValueTuple<,,,,,,>),
            _ => throw new NotSupportedException()
        };
    }
}