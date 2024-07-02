using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

public static class CompositeHandlerCompilation
{
    public static ConstructorInfo GenerateConstructor(
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
        
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        
        Type compositeHandlerInterfaceType = typeof(ICompositeHandler<,,,,>)
            .MakeGenericType(
                dataTypeProjection.DataTupleType,
                dataTypeProjection.HashTupleType,
                dataTypeProjection.BackIndexTupleType,
                dataTypeProjection.ComparerTupleTypes,
                itemType);

        Type resizeHandlerInterfaceType = typeof(IResizeHandler<>).MakeGenericType(dataTypeProjection.DataEntryType);

        var typeBuilder = moduleBuilder.DefineType(
            $"CompositeHandler_{backIndexRank}",
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(ValueType),
            [compositeHandlerInterfaceType, resizeHandlerInterfaceType]);
        
        var hashTableField = DefineConstructor(typeBuilder);
        DefineContains(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType);
        DefineAdd(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType, hashTableField);
        DefineRemove(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType, hashTableField);

        UpdateHandlerCompilation.DefineGetHashCodeAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        UpdateHandlerCompilation.DefineGetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        UpdateHandlerCompilation.DefineSetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);

        var type = typeBuilder.CreateType();

        return type.GetConstructor([typeof(bool)]) ?? throw new InvalidProgramException();
    }

    private static FieldBuilder DefineConstructor(TypeBuilder typeBuilder)
    {
        var hashTableField = typeBuilder.DefineField(
            "_hashTable",
            typeof(HashEntry[]),
            FieldAttributes.InitOnly | FieldAttributes.Private);
        
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Any,
            [typeof(bool)]);
        ILGenerator il = constructorBuilder.GetILGenerator();

        // this
        il.Emit(OpCodes.Ldarg_0);
        // HashEntry.TableMinimalLength
        il.Emit(OpCodes.Ldc_I4, HashEntry.TableMinimalLength);
        // new HashEntry[HashEntry.TableMinimalLength]
        il.Emit(OpCodes.Newarr, typeof(HashEntry));
        // this._hashTable = new HashEntry[HashEntry.TableMinimalLength]
        il.Emit(OpCodes.Stfld, hashTableField);
        
        il.Emit(OpCodes.Ret);

        return hashTableField;
    }

    private static void DefineContains(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type compositeHandlerInterfaceType)
    {
        const string methodName = nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Contains);
        
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(SearchResult),
                [dataTypeProjection.DataTableType, dataTypeProjection.ComparerTupleTypes, typeof(uint), itemType]);
        ILGenerator il = methodBuilder.GetILGenerator();

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Call, typeof(SearchResult).GetMethod(nameof(SearchResult.CreateForItemFound))!);
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, compositeHandlerInterfaceType.GetMethod(methodName)!);
    }
    
    private static void DefineAdd(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type compositeHandlerInterfaceType,
        FieldBuilder hashTableField)
    {
        const string methodName = nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Add);

        Type[] parameterTypes = [
            dataTypeProjection.DataTableType,
            typeof(SearchResult),
            typeof(int),
            typeof(int),
        ];
        
        var updateHandlingType = typeof(UpdateHandling<,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, typeBuilder);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                parameterTypes);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        Label resizeLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        // this._hashTable.Length
        il.Emit(OpCodes.Ldlen);
        // newDataCount
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // HashEntry.IsFullEnough(this._hashTable.Length, newDataCount)
        il.Emit(OpCodes.Call, typeof(HashEntry).GetMethod(nameof(HashEntry.IsFullEnough))!);
        // HashEntry.IsFullEnough(this._hashTable.Length, newDataCount) → resizeLabel
        il.Emit(OpCodes.Brtrue_S, resizeLabel);
        
        var genericAddForUniqueMethod = typeof(UpdateHandling<,>)
            .GetMethod(nameof(UpdateHandling<ValueTuple, UpdateHandlerCompilation.FakeResizeHandler>.AddForUnique))!;
        
        var addForUniqueMethod = TypeBuilder.GetMethod(updateHandlingType, genericAddForUniqueMethod);
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // this
        il.Emit(OpCodes.Ldarg_0);
        // *this
        il.Emit(OpCodes.Ldobj, typeBuilder);
        // lastSearchResult
        il.Emit(OpCodes.Ldarg_2);
        // candidateDataIndex
        il.Emit(OpCodes.Ldarg_3);
        // AddForUnique(this._hashTable, dataTable, *this, lastSearchResult, candidateDataIndex)
        il.Emit(OpCodes.Call, addForUniqueMethod);
        // → endLabel
        il.Emit(OpCodes.Br_S, endLabel);
        
        var genericChangeCapacityForUniqueMethod = typeof(UpdateHandling<,>)
            .GetMethod(nameof(UpdateHandling<ValueTuple, UpdateHandlerCompilation.FakeResizeHandler>.ChangeCapacityForUnique))!;
        
        var changeCapacityForUniqueMethod = TypeBuilder.GetMethod(
            updateHandlingType,
            genericChangeCapacityForUniqueMethod);
        
        il.MarkLabel(resizeLabel);
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // this
        il.Emit(OpCodes.Ldarg_0);
        // *this
        il.Emit(OpCodes.Ldobj, typeBuilder);
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        // this._hashTable.Length
        il.Emit(OpCodes.Ldlen);
        // HashEntry.IncreaseCapacity(this._hashTable.Length)
        il.Emit(OpCodes.Call, typeof(HashEntry).GetMethod(nameof(HashEntry.IncreaseCapacity))!);
        // newDataCount
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // ChangeCapacityForUnique(dataTable, *this, HashEntry.IncreaseCapacity(_hashTable.Length), newDataCount)
        il.Emit(OpCodes.Call, changeCapacityForUniqueMethod);
        // this._hashTable = ChangeCapacityForUnique(…)
        il.Emit(OpCodes.Stfld, hashTableField);
        
        il.MarkLabel(endLabel);
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, compositeHandlerInterfaceType.GetMethod(methodName)!);
    }

    private static void DefineRemove(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type compositeHandlerInterfaceType,
        FieldBuilder hashTableField)
    {
        const string methodName = nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Remove);
        
        Type[] parameterTypes = [dataTypeProjection.DataTableType, typeof(SearchResult), typeof(int)];
        
        var updateHandlingType = typeof(UpdateHandling<,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, typeBuilder);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                parameterTypes);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        Label resizeLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        // this._hashTable.Length
        il.Emit(OpCodes.Ldlen);
        // newDataCount
        il.Emit(OpCodes.Ldarg_3);
        // HashEntry.IsSparseEnough(this._hashTable.Length, newDataCount)
        il.Emit(OpCodes.Call, typeof(HashEntry).GetMethod(nameof(HashEntry.IsSparseEnough))!);
        // HashEntry.IsSparseEnough(this._hashTable.Length, newDataCount) → resizeLabel
        il.Emit(OpCodes.Brtrue_S, resizeLabel);
        
        var genericRemoveForUniqueMethod = typeof(UpdateHandling<,>)
            .GetMethod(nameof(UpdateHandling<ValueTuple, UpdateHandlerCompilation.FakeResizeHandler>.RemoveForUnique))!;
        
        var removeForUniqueMethod = TypeBuilder.GetMethod(updateHandlingType, genericRemoveForUniqueMethod);
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // this
        il.Emit(OpCodes.Ldarg_0);
        // *this
        il.Emit(OpCodes.Ldobj, typeBuilder);
        // successfulSearchResult
        il.Emit(OpCodes.Ldarg_2);
        // newDataCount
        il.Emit(OpCodes.Ldarg_3);
        // AddForUnique(this._hashTable, dataTable, *this, successfulSearchResult, newDataCount)
        il.Emit(OpCodes.Call, removeForUniqueMethod);
        // → endLabel
        il.Emit(OpCodes.Br_S, endLabel);
        
        var genericChangeCapacityForUniqueMethod = typeof(UpdateHandling<,>)
            .GetMethod(nameof(UpdateHandling<ValueTuple, UpdateHandlerCompilation.FakeResizeHandler>.ChangeCapacityForUnique))!;
        
        var changeCapacityForUniqueMethod = TypeBuilder.GetMethod(
            updateHandlingType,
            genericChangeCapacityForUniqueMethod);
        
        il.MarkLabel(resizeLabel);
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // this
        il.Emit(OpCodes.Ldarg_0);
        // *this
        il.Emit(OpCodes.Ldobj, typeBuilder);
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        // this._hashTable.Length
        il.Emit(OpCodes.Ldlen);
        // HashEntry.DecreaseCapacity(this._hashTable.Length)
        il.Emit(OpCodes.Call, typeof(HashEntry).GetMethod(nameof(HashEntry.DecreaseCapacity))!);
        // newDataCount
        il.Emit(OpCodes.Ldarg_3);
        // ChangeCapacityForUnique(dataTable, *this, HashEntry.DecreaseCapacity(_hashTable.Length), newDataCount)
        il.Emit(OpCodes.Call, changeCapacityForUniqueMethod);
        // this._hashTable = ChangeCapacityForUnique(…)
        il.Emit(OpCodes.Stfld, hashTableField);
        
        il.MarkLabel(endLabel);

        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, compositeHandlerInterfaceType.GetMethod(methodName)!);
    }
}