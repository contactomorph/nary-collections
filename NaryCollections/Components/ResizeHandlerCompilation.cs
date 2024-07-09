using System.Reflection.Emit;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

public static class ResizeHandlerCompilation
{
    public struct FakeResizeHandler : IResizeHandler<ValueTuple, int>
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
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(IResizeHandler<object, int>.GetHashCodeAt),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(uint),
                [dataTypeProjection.DataTableType, typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var hashTupleField = CommonCompilation.GetFieldInBase(
            dataTypeProjection.DataEntryType,
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple));
        
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
            il.Emit(OpCodes.Ldfld, indexedField.InputField);
        }

        if (1 < hashMapping.Count)
        {
            // new ValueTuple<…>(…)
            il.Emit(OpCodes.Newobj, hashMapping.OutputType.GetConstructor());
            // EqualityComparerHandling.ComputeTupleHashCode(new ValueTuple<…>(…))
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetTupleHashCodeMethod(hashMapping.OutputType));
        }
        
        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, resizeHandlerInterfaceType, methodBuilder);
    }

    internal static void DefineGetBackIndexAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type resizeHandlerInterfaceType)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(IResizeHandler<object, int>.GetBackIndex),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(int),
                [dataTypeDecomposition.DataTableType, typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        var backIndexesTupleField = CommonCompilation.GetFieldInBase(
            dataTypeDecomposition.DataEntryType,
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.BackIndexesTuple));
        
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

        CommonCompilation.OverrideMethod(typeBuilder, resizeHandlerInterfaceType, methodBuilder);
    }

    internal static void DefineSetBackIndexAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type resizeHandlerInterfaceType)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(IResizeHandler<object, int>.SetBackIndex),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                [dataTypeDecomposition.DataTableType, typeof(int), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        var backIndexesTupleField = CommonCompilation.GetFieldInBase(
            dataTypeDecomposition.DataEntryType,
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.BackIndexesTuple));
        
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

        CommonCompilation.OverrideMethod(typeBuilder, resizeHandlerInterfaceType, methodBuilder);
    }
}