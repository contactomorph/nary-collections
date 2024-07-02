using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Primitives;
using NaryCollections.Tools;

namespace NaryCollections.Components;

public static class UpdateHandlerCompilation
{
    public struct FakeResizeHandler : IResizeHandler<ValueTuple>
    {
        public uint GetHashCodeAt(ValueTuple[] dataTable, int index) => throw new NotImplementedException();

        public int GetBackIndex(ValueTuple[] dataTable, int index) => throw new NotImplementedException();

        public void SetBackIndex(ValueTuple[] dataTable, int index, int backIndex) => throw new NotImplementedException();
    }

    internal static void DefineGetHashCodeAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type resizeHandlerInterfaceType)
    {
        const string methodName = nameof(IResizeHandler<object>.GetHashCodeAt);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(uint),
                [dataTypeProjection.DataTableType, typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var hashTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple))!;
        
        var hashMapping = dataTypeProjection.HashProjectionMapping;
        
        foreach (var indexedField in hashMapping)
        {
            // dataTable
            il.Emit(OpCodes.Ldarg_1);
            // index
            il.Emit(OpCodes.Ldarg_2);
            // &dataTable[index]
            il.Emit(OpCodes.Ldelema, dataTypeProjection.DataEntryType);
            // &dataTable[index].HashTuple
            il.Emit(OpCodes.Ldflda, hashTupleField);
            // dataTable[index].HashTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, indexedField.Field);
        }

        if (1 < hashMapping.Count)
        {
            // new ValueTuple<…>(…)
            il.Emit(OpCodes.Newobj, hashMapping.OutputType.GetConstructor());
            // EqualityComparerHandling.ComputeTupleHashCode(new ValueTuple<…>(…))
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetTupleHashCodeMethod(hashMapping.OutputType));
        }
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, resizeHandlerInterfaceType.GetMethod(methodName)!);
    }

    internal static void DefineGetBackIndexAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type resizeHandlerInterfaceType)
    {
        const string methodName = nameof(IResizeHandler<object>.GetBackIndex);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(int),
                [dataTypeDecomposition.DataTableType, typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        var backIndexesTupleField = dataTypeDecomposition.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.BackIndexesTuple))!;
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].BackIndexesTuple
        il.Emit(OpCodes.Ldflda, backIndexesTupleField);
        // dataTable[index].BackIndexesTuple.Item⟨p⟩
        il.Emit(OpCodes.Ldfld, dataTypeDecomposition.BackIndexProjectionField);
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, resizeHandlerInterfaceType.GetMethod(methodName)!);
    }

    internal static void DefineSetBackIndexAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type resizeHandlerInterfaceType)
    {
        const string methodName = nameof(IResizeHandler<object>.SetBackIndex);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                [dataTypeDecomposition.DataTableType, typeof(int), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        var backIndexesTupleField = dataTypeDecomposition.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.BackIndexesTuple))!;
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].BackIndexesTuple
        il.Emit(OpCodes.Ldflda, backIndexesTupleField);
        // backIndex
        il.Emit(OpCodes.Ldarg_3);
        // dataTable[index].BackIndexesTuple.Item⟨p⟩ = backIndex
        il.Emit(OpCodes.Stfld, dataTypeDecomposition.BackIndexProjectionField);
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, resizeHandlerInterfaceType.GetMethod(methodName)!);
    }
}