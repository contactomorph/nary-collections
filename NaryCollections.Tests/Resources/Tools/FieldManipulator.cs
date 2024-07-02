using System.Linq.Expressions;
using System.Reflection;

namespace NaryCollections.Tests.Resources.Tools;

public sealed class FieldManipulator<T> where T : notnull
{
    private readonly FieldInfo[] _fields;
    
    public FieldManipulator(T instance)
    {
        List<FieldInfo> fields = new();
        var type = instance.GetType();
        while (type is not null)
        {
            fields.AddRange(
                type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
            type = type.BaseType;
        }
        _fields = fields.ToArray();
    }
    
    public void GetFieldValue<TOutput>(T instance, string fieldName, out TOutput output)
    {
        var field = _fields.SingleOrDefault(f => f.Name == fieldName);
        if (field is null)
            throw new MissingFieldException($"Not field of name {fieldName}");
        output = (TOutput)field.GetValue(instance)!;
    }
    
    public void SetFieldValue<TOutput>(T instance, string fieldName, TOutput newValue)
    {
        var field = _fields.SingleOrDefault(f => f.Name == fieldName);
        if (field is null)
            throw new MissingFieldException($"Not field of name {fieldName}");
        field.SetValue(instance, newValue);
    }
    
    public Func<T, TOutput> CreateGetter<TOutput>(string fieldName)
    {
        var field = _fields.SingleOrDefault(f => f.Name == fieldName);
        if (field is null)
            throw new MissingFieldException($"Not field of name {fieldName}");
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