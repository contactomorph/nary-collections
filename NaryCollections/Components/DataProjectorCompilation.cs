using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Primitives;
using NaryCollections.Tools;

namespace NaryCollections.Components;

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
        
        Type projectorInterfaceType = typeof(IDataProjector<,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, itemType);

        var typeBuilder = moduleBuilder.DefineType(
            $"DataProjector_{backIndexRank}",
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(ValueType),
            [projectorInterfaceType]);
        
        var comparerFields = DefineConstructor(typeBuilder, dataTypeProjection.ComparerTypes, projectionIndexes);
        DefineAreDataEqualAt(typeBuilder, dataTypeProjection, projectorInterfaceType, comparerFields);
        DefineComputeHashCode(typeBuilder, dataTypeProjection, projectorInterfaceType, comparerFields);

        Type resizeHandlerInterfaceType = projectorInterfaceType
            .GetInterfaces()
            .Single(i => i.Name.StartsWith(nameof(IResizeHandler<ValueTuple>)));
        
        UpdateHandlerCompilation.DefineGetHashCodeAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        UpdateHandlerCompilation.DefineGetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        UpdateHandlerCompilation.DefineSetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);

        var type = typeBuilder.CreateType();

        return type.GetConstructor(dataTypeProjection.ComparerTypes)!;
    }
    
    private static List<FieldBuilder> DefineConstructor(
        TypeBuilder typeBuilder,
        Type[] comparerTypes,
        byte[] projectionIndexes)
    {
        List<FieldBuilder> comparerFields = new();
        
        foreach (var index in projectionIndexes)
        {
            var comparerField = typeBuilder.DefineField(
                GetComparerFieldName(index),
                comparerTypes[index],
                FieldAttributes.InitOnly);
            comparerFields.Add(comparerField);
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
            il.Emit(OpCodes.Stfld, comparerField);
        }
        il.Emit(OpCodes.Ret);

        return comparerFields;
    }

    private static void DefineAreDataEqualAt(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type projectorInterfaceType,
        IReadOnlyList<FieldBuilder> comparerFields)
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
        
        // hashcode
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // ⟨itemHash⟩ != hashcode → falseLabel
        il.Emit(OpCodes.Bne_Un, falseLabel);
        
        var dataTupleField = dataTypeProjection.DataEntryType.GetField(
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple))!;
        
        var dataMapping = dataTypeProjection.DataProjectionMapping;

        int j = 0;
        foreach (var (_, mappingField) in dataMapping)
        {
            var itemComponentField = dataMapping.OutputType[j];
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // &this._comparer⟨j⟩
            il.Emit(OpCodes.Ldfld, comparerFields[j]);
            // dataTable
            il.Emit(OpCodes.Ldarg_1);
            // index
            il.Emit(OpCodes.Ldarg_2);
            // &dataTable[index]
            il.Emit(OpCodes.Ldelema, dataTypeProjection.DataEntryType);
            // &dataTable[index].DataTuple
            il.Emit(OpCodes.Ldflda, dataTupleField);
            // dataTable[index].DataTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, mappingField);
            // item
            il.Emit(OpCodes.Ldarg_3);
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

        typeBuilder.DefineMethodOverride(methodBuilder, LookForMethod(projectorInterfaceType, methodName));
    }
    
    private static void DefineComputeHashCode(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type projectorInterfaceType,
        IReadOnlyList<FieldBuilder> comparerFields)
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
        
        var dataMappingOutput = dataTypeProjection.DataProjectionMapping.OutputType;
        if (dataMappingOutput.Count == 1)
        {
            var getHashCode = EqualityComparerHandling.GetItemHashCodeMethod(itemType);
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._comparer⟨i⟩
            il.Emit(OpCodes.Ldfld, comparerFields[0]);
            // item
            il.Emit(OpCodes.Ldarg_1);
            // EqualityComparerHandling.Compute⟨Struct|Ref⟩HashCode(this._comparer⟨i⟩, item)
            il.Emit(OpCodes.Call, getHashCode);
        }
        else
        {
            int j = 0;
            foreach (var itemField in dataMappingOutput)
            {
                // this
                il.Emit(OpCodes.Ldarg_0);
                // this._comparer⟨j⟩
                il.Emit(OpCodes.Ldfld, comparerFields[j]);
                // item
                il.Emit(OpCodes.Ldarg_1);
                // item.Item⟨i⟩
                il.Emit(OpCodes.Ldfld, itemField);
                // EqualityComparerHandling.Compute⟨Struct|Ref⟩HashCode(this._comparer⟨j⟩, item.Item⟨j⟩)
                il.Emit(OpCodes.Call, EqualityComparerHandling.GetItemHashCodeMethod(itemField.FieldType));

                ++j;
            }
            
            var itemHashTupleType = ValueTupleType.FromRepeatedComponent<uint>(dataMappingOutput.Count);
            
            // new ValueTuple<…>(…)
            il.Emit(OpCodes.Newobj, itemHashTupleType.GetConstructor());
            // EqualityComparerHandling.ComputeTupleHashCode(new ValueTuple<…>(…))
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetTupleHashCodeMethod(itemHashTupleType));
        }
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, LookForMethod(projectorInterfaceType, methodName));
    }

    private static string GetComparerFieldName(int i) => $"_comparer{i}";

    private static Type GetItemType(DataTypeProjection dataTypeProjection)
    {
        var dataMappingOutput = dataTypeProjection.DataProjectionMapping.OutputType;
        return dataMappingOutput.Count == 1 ? dataMappingOutput[0].FieldType : dataMappingOutput;
    }

    private static MethodInfo LookForMethod(Type interfaceType, string methodName)
    {
        var method = interfaceType.GetMethod(methodName);
        if (method is not null)
            return method;
        foreach (var superType in interfaceType.GetInterfaces())
        {
            method = superType.GetMethod(methodName);
            if (method is not null)
                return method;
        }

        throw new MissingMethodException($"Missing method {methodName}");
    }
}
