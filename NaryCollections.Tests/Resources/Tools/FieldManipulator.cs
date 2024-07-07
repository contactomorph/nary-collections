using System.Linq.Expressions;
using System.Reflection;

namespace NaryCollections.Tests.Resources.Tools;

public sealed class FieldManipulator<T> where T : notnull
{
    private readonly IReadOnlySet<FieldInfo> _fields;
    
    public FieldManipulator(T instance)
    {
        Dictionary<string, FieldInfo> fieldsByName = new();
        var type = instance.GetType();
        while (type is not null)
        {
            var fields = type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (!fieldsByName.TryAdd(field.Name, field))
                {
                    var existing = fieldsByName[field.Name];
                    if (existing.DeclaringType != field.DeclaringType || existing.FieldType != field.FieldType)
                        throw new AmbiguousMatchException($"Multiple fields of name {field.Name}");
                }
            }
            type = type.BaseType;
        }
        _fields = fieldsByName.Values.ToHashSet();
    }
    
    public void GetFieldValue<TOutput>(T instance, string fieldName, out TOutput output)
    {
        var fields = _fields.Where(f => f.Name == fieldName).Take(2).ToArray();
        output = fields.Length switch
        {
            0 => throw new MissingFieldException($"Not field of name {fieldName}"),
            1 => (TOutput)fields[0].GetValue(instance)!,
            _ => throw new AmbiguousMatchException($"Multiple fields of name {fieldName}")
        };
    }
    
    public void SetFieldValue<TOutput>(T instance, string fieldName, TOutput newValue)
    {
        var fields = _fields.Where(f => f.Name == fieldName).Take(2).ToArray();
        var field = fields.Length switch
        {
            0 => throw new MissingFieldException($"Not field of name {fieldName}"),
            1 => fields[0],
            _ => throw new AmbiguousMatchException($"Multiple fields of name {fieldName}")
        };
        field.SetValue(instance, newValue);
    }
    
    public Func<T, TOutput> CreateGetter<TOutput>(string fieldName)
    {
        var fields = _fields.Where(f => f.Name == fieldName).Take(2).ToArray();
        var field = fields.Length switch
        {
            0 => throw new MissingFieldException($"Not field of name {fieldName}"),
            1 => fields[0],
            _ => throw new AmbiguousMatchException($"Multiple fields of name {fieldName}")
        };
        var instance = Expression.Parameter(typeof(T), "instance");
        var downcastInstance = Expression.Convert(instance, field.DeclaringType!);
        var body = Expression.Convert(Expression.Field(downcastInstance, field), typeof(TOutput));
        return Expression.Lambda<Func<T, TOutput>>(body, instance).Compile();
    }
}

public static class FieldManipulator
{
    public static FieldManipulator<T> ForRealTypeOf<T>(T instance) where T : notnull => new(instance);
}