using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

public static class CompositeHandlerCompilation
{
    public const string HashTableFieldName = "_hashTable";
    public static string GetCompositeHandlerFieldName(byte backIndexRank) => $"CompositeHandler_{backIndexRank}";
    
    public static ConstructorInfo GenerateConstructor(
        ModuleBuilder moduleBuilder,
        Type dataTupleType,
        byte[] projectionIndexes,
        byte backIndexRank,
        bool[] backIndexMultiplicity)
    {
        var dataTypeProjection = new DataTypeProjection(
            dataTupleType,
            backIndexRank,
            backIndexMultiplicity,
            projectionIndexes);
        
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        
        Type compositeHandlerInterfaceType = typeof(ICompositeHandler<,,,,>)
            .MakeGenericType(
                dataTypeProjection.DataTupleType,
                dataTypeProjection.HashTupleType,
                dataTypeProjection.BackIndexTupleType,
                dataTypeProjection.ComparerTupleType,
                itemType);

        Type resizeHandlerInterfaceType = typeof(IResizeHandler<,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, typeof(int));
        Type dataEquatorInterfaceType = typeof(IDataEquator<,,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, dataTypeProjection.ComparerTupleType, itemType);

        var typeBuilder = moduleBuilder.DefineType(
            GetCompositeHandlerFieldName(backIndexRank),
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(ValueType),
            [compositeHandlerInterfaceType, resizeHandlerInterfaceType, dataEquatorInterfaceType]);
        
        var hashTableField = DefineConstructor(typeBuilder);
        DefineFind(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType, hashTableField);
        DefineAdd(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType, hashTableField);
        DefineRemove(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType, hashTableField);
        DefineClear(typeBuilder, compositeHandlerInterfaceType, hashTableField);

        ResizeHandlerCompilation.DefineGetHashCodeAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        ResizeHandlerCompilation.DefineGetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        ResizeHandlerCompilation.DefineSetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        DataEquatorCompilation.DefineAreDataEqualAt(typeBuilder, dataTypeProjection, dataEquatorInterfaceType);

        var type = typeBuilder.CreateType();

        return type.GetConstructor([typeof(bool)]) ?? throw new InvalidProgramException();
    }

    private static FieldBuilder DefineConstructor(TypeBuilder typeBuilder)
    {
        var hashTableField = typeBuilder.DefineField(
            HashTableFieldName,
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

    private static void DefineFind(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type compositeHandlerInterfaceType,
        FieldBuilder hashTableField)
    {
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        
        var updateHandlingType = typeof(MembershipHandling<,,,>)
            .MakeGenericType(
                dataTypeProjection.DataEntryType,
                dataTypeProjection.ComparerTupleType,
                itemType,
                typeBuilder);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Find),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(SearchResult),
                [dataTypeProjection.DataTableType, dataTypeProjection.ComparerTupleType, typeof(uint), itemType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var genericFindMethod = typeof(MembershipHandling<,,,>)
            .GetMethod(nameof(MembershipHandling<ValueTuple, ValueTuple, object, DataEquatorCompilation.FakeDataEquator>.Find))!;
        
        var findMethod = TypeBuilder.GetMethod(updateHandlingType, genericFindMethod);

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
        // comparerTuple
        il.Emit(OpCodes.Ldarg_2);
        // candidateHashCode
        il.Emit(OpCodes.Ldarg_3);
        // candidateItem
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // MembershipHandling<…>.Find(this._hashTable, dataTable, *this, comparerTuple, candidateHashCode, candidateItem)
        il.Emit(OpCodes.Call, findMethod);
        
        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, compositeHandlerInterfaceType, methodBuilder);
    }
    
    private static void DefineAdd(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type compositeHandlerInterfaceType,
        FieldBuilder hashTableField)
    {
        Type[] parameterTypes = [
            dataTypeProjection.DataTableType,
            typeof(SearchResult),
            typeof(int),
            typeof(int),
        ];

        var updateHandlingTypeDefinition = typeof(MonoUpdateHandling<,>);
        var updateHandlingType = updateHandlingTypeDefinition
            .MakeGenericType(dataTypeProjection.DataEntryType, typeBuilder);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Add),
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

        var genericAddMethod = updateHandlingTypeDefinition
            .GetMethod(nameof(MonoUpdateHandling<ValueTuple, ResizeHandlerCompilation.FakeResizeHandler>.Add))!;
        
        var addMethod = TypeBuilder.GetMethod(updateHandlingType, genericAddMethod);
        
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
        // Add(this._hashTable, dataTable, *this, lastSearchResult, candidateDataIndex)
        il.Emit(OpCodes.Call, addMethod);
        // → endLabel
        il.Emit(OpCodes.Br_S, endLabel);
        
        var genericChangeCapacityMethod = updateHandlingTypeDefinition
            .GetMethod(nameof(MonoUpdateHandling<ValueTuple, ResizeHandlerCompilation.FakeResizeHandler>.ChangeCapacity))!;
        
        var changeCapacityMethod = TypeBuilder.GetMethod(updateHandlingType, genericChangeCapacityMethod);
        
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
        // ChangeCapacity(dataTable, *this, HashEntry.IncreaseCapacity(_hashTable.Length), newDataCount)
        il.Emit(OpCodes.Call, changeCapacityMethod);
        // this._hashTable = ChangeCapacity(…)
        il.Emit(OpCodes.Stfld, hashTableField);
        
        il.MarkLabel(endLabel);
        
        il.Emit(OpCodes.Ret);
        
        CommonCompilation.OverrideMethod(typeBuilder, compositeHandlerInterfaceType, methodBuilder);
    }

    private static void DefineRemove(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type compositeHandlerInterfaceType,
        FieldBuilder hashTableField)
    {
        Type[] parameterTypes = [dataTypeProjection.DataTableType, typeof(SearchResult), typeof(int)];
        
        var updateHandlingTypeDefinition = typeof(MonoUpdateHandling<,>);
        var updateHandlingType = updateHandlingTypeDefinition
            .MakeGenericType(dataTypeProjection.DataEntryType, typeBuilder);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Remove),
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
        
        var genericRemoveMethod = updateHandlingTypeDefinition
            .GetMethod(nameof(MonoUpdateHandling<ValueTuple, ResizeHandlerCompilation.FakeResizeHandler>.Remove))!;
        
        var removeMethod = TypeBuilder.GetMethod(updateHandlingType, genericRemoveMethod);
        
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
        // Remove(this._hashTable, dataTable, *this, successfulSearchResult, newDataCount)
        il.Emit(OpCodes.Call, removeMethod);
        // → endLabel
        il.Emit(OpCodes.Br_S, endLabel);
        
        var genericChangeCapacityMethod = updateHandlingTypeDefinition
            .GetMethod(nameof(MonoUpdateHandling<ValueTuple, ResizeHandlerCompilation.FakeResizeHandler>.ChangeCapacity))!;
        
        var changeCapacityForUniqueMethod = TypeBuilder.GetMethod(
            updateHandlingType,
            genericChangeCapacityMethod);
        
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
        // ChangeCapacity(dataTable, *this, HashEntry.DecreaseCapacity(_hashTable.Length), newDataCount)
        il.Emit(OpCodes.Call, changeCapacityForUniqueMethod);
        // this._hashTable = ChangeCapacity(…)
        il.Emit(OpCodes.Stfld, hashTableField);
        
        il.MarkLabel(endLabel);

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, compositeHandlerInterfaceType, methodBuilder);
    }
    
    private static void DefineClear(
        TypeBuilder typeBuilder,
        Type compositeHandlerInterfaceType,
        FieldBuilder hashTableField)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Clear),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                []);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // HashEntry.TableMinimalLength
        il.Emit(OpCodes.Ldc_I4, HashEntry.TableMinimalLength);
        // new HashEntry[HashEntry.TableMinimalLength]
        il.Emit(OpCodes.Newarr, typeof(HashEntry));
        // this._hashTable = new HashEntry[HashEntry.TableMinimalLength]
        il.Emit(OpCodes.Stfld, hashTableField);

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, compositeHandlerInterfaceType, methodBuilder);
    }
}