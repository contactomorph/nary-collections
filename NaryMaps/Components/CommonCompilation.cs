using System.Reflection;
using System.Reflection.Emit;
using NaryMaps.Primitives;

namespace NaryMaps.Components;

internal static class CommonCompilation
{
    public static readonly MethodAttributes ProjectorMethodAttributes =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final;

    public static readonly BindingFlags BaseFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static readonly MethodInfo GetCaseMethod =
        typeof(SearchResult).GetProperty(nameof(SearchResult.Case))!.GetGetMethod()!;
    
    public static Type GetItemType(DataTypeProjection dataTypeProjection)
    {
        var dataMappingOutput = dataTypeProjection.DataProjectionMapping.OutputType;
        return dataMappingOutput.Count == 1 ? dataMappingOutput[0].FieldType : dataMappingOutput;
    }

    public static void OverrideMethod(TypeBuilder typeBuilder, Type upperType, MethodBuilder methodBuilder)
    {
        var method = upperType.GetMethod(methodBuilder.Name, BaseFlags) ??
                     throw new MissingMethodException();
        
        typeBuilder.DefineMethodOverride(methodBuilder, method);
    }

    public static FieldInfo GetFieldInBase(Type baseType, string fieldName)
    {
        return baseType.GetField(fieldName, BaseFlags) ?? throw new MissingFieldException();
    }
    
    public static MethodInfo GetMethod(Type type, string methodName)
    {
        return type.GetMethod(methodName, BaseFlags) ?? throw new MissingMethodException();
    }
}