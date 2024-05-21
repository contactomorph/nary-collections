using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NaryCollections.Details;

public static class DataProjectorCompilation
{
    private static readonly MethodAttributes ProjectorMethodAttributes =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final;

    public static ConstructorInfo GenerateProjectorConstructor(
        ModuleBuilder moduleBuilder,
        Type dataTupleType,
        int[] projectionIndexes,
        byte backIndexRank,
        byte backIndexCount)
    {
        var dataTypeDecomposition = new DataTypeProjection(
            dataTupleType,
            backIndexRank,
            backIndexCount,
            projectionIndexes);
        
        var itemType = dataTypeDecomposition.DataProjectionFields.Length == 1 ?
            dataTypeDecomposition.DataProjectionTypes[0] :
            TupleHandling.CreateTupleType(dataTypeDecomposition.DataProjectionTypes);
        
        var projectorInterfaceType = typeof(IDataProjector<,>)
            .MakeGenericType(dataTypeDecomposition.DataEntryType, itemType);

        var projectorTypeName = NameProjectorType(backIndexRank, dataTypeDecomposition.DataTypes);

        var typeBuilder = moduleBuilder.DefineType(
            projectorTypeName,
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(object),
            [projectorInterfaceType]);

        var comparerFields = DefineConstructor(typeBuilder, dataTypeDecomposition.ComparerTypes, projectionIndexes);
        DefineGetDataAt(typeBuilder, itemType, dataTypeDecomposition, projectorInterfaceType);
        DefineSetDataAt(typeBuilder, itemType, dataTypeDecomposition, projectorInterfaceType);
        DefineAreDataEqualAt(typeBuilder, itemType, dataTypeDecomposition, projectorInterfaceType, comparerFields);
        DefineGetBackIndexAt(typeBuilder, backIndexRank, dataTypeDecomposition, projectorInterfaceType);
        DefineSetBackIndexAt(typeBuilder, backIndexRank, dataTypeDecomposition, projectorInterfaceType);

        var type = typeBuilder.CreateType();

        return type.GetConstructor(dataTypeDecomposition.ComparerTypes)!;
    }

    private static List<(FieldBuilder Field, int Index)> DefineConstructor(
        TypeBuilder typeBuilder,
        Type[] comparerTypes,
        int[] projectionIndexes)
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
        Type itemType,
        DataTypeProjection dataTypeDecomposition,
        Type projectorInterfaceType)
    {
        const string methodName = nameof(IDataProjector<object, object>.GetDataAt);
        var returnType = typeof(ValueTuple<,>).MakeGenericType(itemType, typeof(uint));
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                returnType,
                [dataTypeDecomposition.DataTableType, typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (dataTypeDecomposition.DataProjectionFields.Length != 1)
            throw new NotSupportedException();
        
        var dataTupleField = dataTypeDecomposition.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple))!;
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].DataTuple
        il.Emit(OpCodes.Ldflda, dataTupleField);
        // dataTable[index].DataTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeDecomposition.DataProjectionFields[0]);
        
        var hashTupleField = dataTypeDecomposition.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple))!;
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].HashTuple
        il.Emit(OpCodes.Ldflda, hashTupleField);
        // dataTable[index].HashTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeDecomposition.HashProjectionFields[0]);
        
        // new ValueTuple<…, uint>(ataTable[index].DataTuple.Item⟨i⟩, dataTable[index].HashTuple.Item⟨i⟩)
        il.Emit(OpCodes.Newobj, returnType.GetConstructors().Single());
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(
            methodBuilder,
            projectorInterfaceType.GetMethod(methodName)!);
    }
    
    private static void DefineSetDataAt(
        TypeBuilder typeBuilder,
        Type itemType,
        DataTypeProjection dataTypeDecomposition,
        Type projectorInterfaceType)
    {
        const string methodName = nameof(IDataProjector<object, object>.SetDataAt);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(void),
                [dataTypeDecomposition.DataTableType, typeof(int), itemType, typeof(uint)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (dataTypeDecomposition.DataProjectionFields.Length != 1)
            throw new NotSupportedException();
        
        var dataTupleField = dataTypeDecomposition.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple))!;

        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].DataTuple
        il.Emit(OpCodes.Ldflda, dataTupleField);
        // item
        il.Emit(OpCodes.Ldarg_3);
        // dataTable[index].DataTuple.Item⟨i⟩ = item
        il.Emit(OpCodes.Stfld, dataTypeDecomposition.DataProjectionFields[0]);
        
        var hashTupleField = dataTypeDecomposition.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple))!;
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].HashTuple
        il.Emit(OpCodes.Ldflda, hashTupleField);
        // hashcode
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // dataTable[index].HashTuple.Item⟨i⟩ = hashcode
        il.Emit(OpCodes.Stfld, dataTypeDecomposition.HashProjectionFields[0]);

        il.Emit(OpCodes.Ret);
        
        typeBuilder.DefineMethodOverride(
            methodBuilder,
            projectorInterfaceType.GetMethod(methodName)!);
    }
    
    private static void DefineAreDataEqualAt(TypeBuilder typeBuilder,
        Type itemType,
        DataTypeProjection dataTypeDecomposition,
        Type projectorInterfaceType,
        IReadOnlyList<(FieldBuilder Field, int Index)> comparerFields)
    {
        const string methodName = nameof(IDataProjector<object, object>.AreDataEqualAt);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(bool),
                [dataTypeDecomposition.DataTableType, typeof(int), itemType, typeof(uint)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        if (comparerFields.Count != 1)
            throw new NotSupportedException();
        
        var hashTupleField = dataTypeDecomposition.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.HashTuple))!;
        
        Label hashCodeAreDifferentLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();
        
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // index
        il.Emit(OpCodes.Ldarg_2);
        // &dataTable[index]
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].HashTuple
        il.Emit(OpCodes.Ldflda, hashTupleField);
        // dataTable[index].HashTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeDecomposition.HashProjectionFields[0]);
        // hashcode
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // dataTable[index].HashTuple.Item⟨i⟩ != hashcode → hashCodeAreDifferentLabel
        il.Emit(OpCodes.Bne_Un, hashCodeAreDifferentLabel);
        
        var dataTupleField = dataTypeDecomposition.DataEntryType.GetField(
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
        il.Emit(OpCodes.Ldelema, dataTypeDecomposition.DataEntryType);
        // &dataTable[index].DataTuple
        il.Emit(OpCodes.Ldflda, dataTupleField);
        // dataTable[index].DataTuple.Item⟨i⟩
        il.Emit(OpCodes.Ldfld, dataTypeDecomposition.DataProjectionFields[0]);
        // item
        il.Emit(OpCodes.Ldarg_3);
        // this._comparer⟨i⟩.Equals(dataTable[index].DataTuple.Item⟨i⟩, item)
        il.Emit(OpCodes.Callvirt, dataTypeDecomposition.ComparerMethods[comparerFields[0].Index]);
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
        byte backIndexRank,
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
        
        if (dataTypeDecomposition.DataProjectionFields.Length != 1)
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
        byte backIndexRank,
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
        
        if (dataTypeDecomposition.DataProjectionFields.Length != 1)
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

    private static string GetComparerFieldName(int i) => $"_comparer{i}";
    
    private static string NameProjectorType(byte backIndexRank, Type[] types)
    {
        var projectorTypeName = new StringBuilder("DataProjector_");
        foreach (var type in types)
        {
            projectorTypeName.Append(type.Name.Split('`')[0]);
        }
        projectorTypeName.Append('_').Append(backIndexRank);
        return projectorTypeName.ToString();
    }
}
