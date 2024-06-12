using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Tools;

namespace NaryCollections.Details;

public static class DataProjectorCompilation
{
    private static readonly MethodAttributes ProjectorMethodAttributes =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final;

    public static ConstructorInfo GenerateProjectorConstructor(
        ModuleBuilder moduleBuilder,
        Type dataTupleType,
        byte[] projectionIndexes,
        byte backIndexRank,
        byte backIndexCount)
    {
        var dataTypeProjection = new DataTypeProjection(
            dataTupleType,
            backIndexRank,
            backIndexCount,
            projectionIndexes);
        
        var itemType = GetItemType(dataTypeProjection);
        
        var projectorInterfaceType = typeof(IDataProjector<,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, itemType);

        var typeBuilder = moduleBuilder.DefineType(
            $"DataProjector_{backIndexRank}",
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(object),
            [projectorInterfaceType]);
        
        var comparerFields = DefineConstructor(typeBuilder, dataTypeProjection.ComparerTypes, projectionIndexes);
        DefineGetDataAt(typeBuilder, dataTypeProjection, projectorInterfaceType);
        DefineAreDataEqualAt(typeBuilder, dataTypeProjection, projectorInterfaceType, comparerFields);
        DefineGetBackIndexAt(typeBuilder, dataTypeProjection, projectorInterfaceType);
        DefineSetBackIndexAt(typeBuilder, dataTypeProjection, projectorInterfaceType);
        DefineComputeHashCode(typeBuilder, dataTypeProjection, projectorInterfaceType, comparerFields);

        var type = typeBuilder.CreateType();

        return type.GetConstructor(dataTypeProjection.ComparerTypes)!;
    }

    private static List<(FieldBuilder Field, int Index)> DefineConstructor(
        TypeBuilder typeBuilder,
        Type[] comparerTypes,
        byte[] projectionIndexes)
    {
        List<(FieldBuilder Field, int Index)> comparerFields = new();
        
        foreach (var index in projectionIndexes)
        {
            var comparerField = typeBuilder.DefineField(
                GetComparerFieldName(index),
                comparerTypes[index],
                FieldAttributes.InitOnly);
            comparerFields.Add((comparerField, index));
        }

        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Any,
            comparerTypes);
        ILGenerator il = constructorBuilder.GetILGenerator();

        for (int i = 0; i < projectionIndexes.Length; ++i)
        {
            var b = (byte)(projectionIndexes[i] + 1);
            var comparerField = comparerFields[i];
            // this
            il.Emit(OpCodes.Ldarg_0);
            // comparer⟨b⟩
            il.Emit(OpCodes.Ldarg_S, b);
            // this._comparer⟨b⟩ = comparer⟨b⟩
            il.Emit(OpCodes.Stfld, comparerField.Field);
        }
        il.Emit(OpCodes.Ret);

        return comparerFields;
    }

    private static void DefineGetDataAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type projectorInterfaceType)
    {
        const string methodName = nameof(IDataProjector<object, object>.GetDataAt);
        
        var itemType = GetItemType(dataTypeProjection);
        var returnType = ValueTupleType.FromComponents(itemType, typeof(uint));
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                returnType,
                [dataTypeProjection.DataTableType, typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (dataTypeProjection.DataProjectionMapping.OutputType.Count != 1)
            throw new NotSupportedException();
        
        var dataTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple))!;
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeProjection.DataEntryType);
        // &dataTable[index].DataTuple
        il.Emit(OpCodes.Ldflda, dataTupleField);
        // dataTable[index].DataTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeProjection.DataProjectionMapping.MappingFields[0]);
        
        var hashTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple))!;

        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeProjection.DataEntryType);
        // &dataTable[index].HashTuple
        il.Emit(OpCodes.Ldflda, hashTupleField);
        // dataTable[index].HashTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeProjection.HashProjectionMapping.MappingFields[0]);
        
        // new ValueTuple<…, uint>(…, …)
        il.Emit(OpCodes.Newobj, returnType.GetConstructor());
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(
            methodBuilder,
            projectorInterfaceType.GetMethod(methodName)!);
    }
    
    private static void DefineAreDataEqualAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type projectorInterfaceType,
        IReadOnlyList<(FieldBuilder Field, int Index)> comparerFields)
    {
        const string methodName = nameof(IDataProjector<object, object>.AreDataEqualAt);
        
        var itemType = GetItemType(dataTypeProjection);
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(bool),
                [dataTypeProjection.DataTableType, typeof(int), itemType, typeof(uint)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (comparerFields.Count != 1)
            throw new NotSupportedException();
        
        var hashTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple))!;
        
        Label hashCodeAreDifferentLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeProjection.DataEntryType);
        // &dataTable[index].HashTuple
        il.Emit(OpCodes.Ldflda, hashTupleField);
        // dataTable[index].HashTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeProjection.HashProjectionMapping.MappingFields[0]);
        // hashcode
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // dataTable[index].HashTuple.Item⟨i⟩ != hashcode → hashCodeAreDifferentLabel
        il.Emit(OpCodes.Bne_Un, hashCodeAreDifferentLabel);
        
        var dataTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple))!;
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // &this._comparer⟨i⟩
        il.Emit(OpCodes.Ldfld, comparerFields[0].Field);
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeProjection.DataEntryType);
        // &dataTable[index].DataTuple
        il.Emit(OpCodes.Ldflda, dataTupleField);
        // dataTable[index].DataTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeProjection.DataProjectionMapping.MappingFields[0]);
        // item
        il.Emit(OpCodes.Ldarg_3);
        // EqualityComparerHandling.Equals(this._comparer⟨i⟩, dataTable[index].DataTuple.Item⟨i⟩, item)
        il.Emit(OpCodes.Call, EqualityComparerHandling.GetEqualsMethod(itemType));
        // → endLabel
        il.Emit(OpCodes.Br_S, endLabel);
        
        il.MarkLabel(hashCodeAreDifferentLabel);
        // false
        il.Emit(OpCodes.Ldc_I4_0);
        
        il.MarkLabel(endLabel);
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(
            methodBuilder,
            projectorInterfaceType.GetMethod(methodName)!);
    }

    private static void DefineGetBackIndexAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type projectorInterfaceType)
    {
        const string methodName = nameof(IDataProjector<object, object>.GetBackIndex);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(int),
                [dataTypeDecomposition.DataTableType, typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (dataTypeDecomposition.DataProjectionMapping.OutputType.Count != 1)
            throw new NotSupportedException();

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

        typeBuilder.DefineMethodOverride(
            methodBuilder,
            projectorInterfaceType.GetMethod(methodName)!);
    }

    private static void DefineSetBackIndexAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type projectorInterfaceType)
    {
        const string methodName = nameof(IDataProjector<object, object>.SetBackIndex);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(void),
                [dataTypeDecomposition.DataTableType, typeof(int), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (dataTypeDecomposition.DataProjectionMapping.OutputType.Count != 1)
            throw new NotSupportedException();

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

        typeBuilder.DefineMethodOverride(
            methodBuilder,
            projectorInterfaceType.GetMethod(methodName)!);
    }
    
    private static void DefineComputeHashCode(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type projectorInterfaceType,
        IReadOnlyList<(FieldBuilder Field, int Index)> comparerFields)
    {
        const string methodName = nameof(IDataProjector<object, object>.ComputeHashCode);
        
        var itemType = GetItemType(dataTypeProjection);
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(uint),
                [itemType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (dataTypeProjection.DataProjectionMapping.OutputType.Count != 1)
            throw new NotSupportedException();
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._comparer⟨i⟩
        il.Emit(OpCodes.Ldfld, comparerFields[0].Field);
        // item
        il.Emit(OpCodes.Ldarg_1);
        // EqualityComparerHandling.Compute⟨…⟩HashCode(this._comparer⟨i⟩, item)
        il.Emit(OpCodes.Call, EqualityComparerHandling.GetItemHashCodeMethod(itemType));
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(
            methodBuilder,
            projectorInterfaceType.GetMethod(methodName)!);
    }

    private static string GetComparerFieldName(int i) => $"_comparer{i}";

    private static Type GetItemType(DataTypeProjection dataTypeProjection)
    {
        var dataMappingOutput = dataTypeProjection.DataProjectionMapping.OutputType;
        return dataMappingOutput.Count == 1 ? dataMappingOutput[0].FieldType : dataMappingOutput;
    }
}
