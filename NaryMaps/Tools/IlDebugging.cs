using System.Reflection;
using System.Reflection.Emit;

namespace NaryMaps.Tools;

public static class IlDebugging
{
    public static void DisplayAndPopLastValue(ILGenerator il, Type type, string? dataName = null)
    {
        bool byRef = type.IsByRef;
        if (byRef)
        {
            il.Emit(OpCodes.Ldind_Ref);
            type = type.GetElementType()!;
        }

        il.Emit(byRef ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        if (dataName is null)
            il.Emit(OpCodes.Ldnull);
        else
            il.Emit(OpCodes.Ldstr, dataName);
        il.Emit(OpCodes.Call, DisplayMethodDefinition.MakeGenericMethod(type));
    }
    
    public static void DisplayLastValue(ILGenerator il, Type type, string? dataName = null)
    {
        var local = il.DeclareLocal(type);
        
        bool byRef = type.IsByRef;
        
        il.Emit(OpCodes.Stloc, local);
        il.Emit(OpCodes.Ldloc, local);
        if (byRef)
        {
            il.Emit(OpCodes.Ldind_Ref);
            type = type.GetElementType()!;
        }
        il.Emit(byRef ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        if (dataName is null)
            il.Emit(OpCodes.Ldnull);
        else
            il.Emit(OpCodes.Ldstr, dataName);
        il.Emit(OpCodes.Call, DisplayMethodDefinition.MakeGenericMethod(type));
        il.Emit(OpCodes.Ldloc, local);
    }

    public static void DisplayValues<T>(ILGenerator il, IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            il.Emit(OpCodes.Ldstr, $"- {value}");
            il.Emit(OpCodes.Call, DisplayTextMethod);
        }
        il.Emit(OpCodes.Ldstr, "");
        il.Emit(OpCodes.Call, DisplayTextMethod);
    }

    private static readonly MethodInfo DisplayMethodDefinition = typeof(IlDebugging).GetMethod(nameof(_Display))!;
    
    private static readonly MethodInfo ToStringMethod = typeof(object).GetMethod(nameof(ToString))!;

    public static void _Display<T>(T value, bool byRef, string? dataName)
    {
        string? valueText = null;
        if (value is not null)
        {
            if (value.GetType().IsArray)
            {
                var array = (Array)(object)value;
                valueText = array.Length == 0 ? "[]" : $"[{array.GetValue(0)} … \u00d7 {array.Length}]";
            }
            else
            {
                Delegate del = value.ToString;
                valueText = del.GetMethodInfo() != ToStringMethod ? value.ToString() : "non-specific";
            }
        }

        string prefix = byRef ? "ref " : string.Empty;
        Console.WriteLine($"Display {dataName}");
        Console.WriteLine($"- Type:  {prefix}{typeof(T)}");
        Console.WriteLine($"- Value: « {valueText} »");
        Console.WriteLine();
    }

    private static readonly MethodInfo DisplayTextMethod = typeof(IlDebugging).GetMethod(nameof(_DisplayText))!;

    public static void _DisplayText(string text) => Console.WriteLine(text);
}