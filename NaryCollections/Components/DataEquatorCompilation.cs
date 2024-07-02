using System.Reflection.Emit;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

internal static class DataEquatorCompilation
{
    public static void DefineAreDataEqualAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type dataEquatorInterfaceType)
    {
        const string methodName = nameof(IDataEquator<object, ValueTuple, object>.AreDataEqualAt);
        
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        
        Type[] parameterTypes = [
            dataTypeProjection.DataTableType,
            dataTypeProjection.ComparerTupleTypes,
            typeof(int),
            itemType,
            typeof(uint),
        ];
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(bool),
                parameterTypes);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        Label falseLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();
        
        var hashTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple))!;
        
        var hashMapping = dataTypeProjection.HashProjectionMapping;
        
        foreach (var indexedField in hashMapping)
        {
            // dataTable
            il.Emit(OpCodes.Ldarg_1);
            // index
            il.Emit(OpCodes.Ldarg_3);
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
        
        // hashcode
        il.Emit(OpCodes.Ldarg_S, (byte)5);
        // ⟨itemHash⟩ != hashcode → falseLabel
        il.Emit(OpCodes.Bne_Un, falseLabel);
        
        var dataTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple))!;
        
        var dataMapping = dataTypeProjection.DataProjectionMapping;

        int j = 0;
        foreach (var (i, mappingField) in dataMapping)
        {
            var itemComponentField = dataMapping.OutputType[j];
            var comparerField = dataTypeProjection.ComparerTupleTypes[i];
            
            // comparerTuple
            il.Emit(OpCodes.Ldarg_2);
            // comparerTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, comparerField);
            // dataTable
            il.Emit(OpCodes.Ldarg_1);
            // index
            il.Emit(OpCodes.Ldarg_3);
            // &dataTable[index]
            il.Emit(OpCodes.Ldelema, dataTypeProjection.DataEntryType);
            // &dataTable[index].DataTuple
            il.Emit(OpCodes.Ldflda, dataTupleField);
            // dataTable[index].DataTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, mappingField);
            // item
            il.Emit(OpCodes.Ldarg_S, (byte)4);
            if (1 < dataMapping.Count)
            {
                // item.Item⟨j⟩
                il.Emit(OpCodes.Ldfld, itemComponentField);
            }
            // EqualityComparerHandling.ComputeEquals(⟨comparer⟩, dataTable[index].DataTuple.Item⟨i⟩, ⟨itemComponent⟩)
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetEqualsMethod(itemComponentField.FieldType));
            // EqualityComparerHandling.ComputeEquals(⟨comparer⟩, ⟨dataComponent⟩, ⟨itemComponent⟩) → falseLabel
            il.Emit(OpCodes.Brfalse_S, falseLabel);
        
            ++j;
        }
        // true
        il.Emit(OpCodes.Ldc_I4_1);
        // → endLabel
        il.Emit(OpCodes.Br_S, endLabel);
        
        il.MarkLabel(falseLabel);
        // false
        il.Emit(OpCodes.Ldc_I4_0);
        
        il.MarkLabel(endLabel);
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, dataEquatorInterfaceType.GetMethod(methodName)!);
    }
}