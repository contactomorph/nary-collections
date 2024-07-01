using System.Linq.Expressions;
using System.Reflection;

namespace NaryCollections.Tests.Resources.Tools;

public static class FieldHelpers
{
    public static List<FieldInfo> GetInstanceFields(object instance)
    {
        List<FieldInfo> fields = new();
        var type = instance.GetType();
        while (type is not null)
        {
            fields.AddRange(
                type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
            type = type.BaseType;
        }

        return fields;
    }
    
    public static Func<TInput, TOutput> CreateGetter<TInput, TOutput>(IReadOnlyList<FieldInfo> fields, string name)
    {
        var field = fields.SingleOrDefault(f => f.Name == name);
        if (field is null)
            throw new MissingFieldException($"Not field of name {name}");
        var instance = Expression.Parameter(typeof(TInput), "instance");
        var downcastInstance = Expression.Convert(instance, field.DeclaringType!);
        var body = Expression.Convert(Expression.Field(downcastInstance, field), typeof(TOutput));
        return Expression.Lambda<Func<TInput, TOutput>>(body, instance).Compile();
    }
    
    public static TOutput GetFieldValue<TOutput>(IReadOnlyList<FieldInfo> fields, string name, object instance)
    {
        var field = fields.SingleOrDefault(f => f.Name == name);
        if (field is null)
            throw new MissingFieldException($"Not field of name {name}");
        return (TOutput)field.GetValue(instance)!;
    }
}