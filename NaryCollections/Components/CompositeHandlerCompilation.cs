using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Fakes;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

using MonoUpdateHandling = MonoUpdateHandling<ValueTuple, FakeResizeHandler>;
using MultiUpdateHandling = MultiUpdateHandling<ValueTuple, FakeResizeHandler>;
using MembershipHandling = MembershipHandling<ValueTuple, ValueTuple, object, FakeDataEquator>;
using ICompositeHandler = ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>;

public static class CompositeHandlerCompilation
{
    public const string HashTableFieldName = "_hashTable";
    public const string CountFieldName = "_count";
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
            .MakeGenericType(dataTypeProjection.DataEntryType, dataTypeProjection.BackIndexType);
        Type dataEquatorInterfaceType = typeof(IDataEquator<,,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, dataTypeProjection.ComparerTupleType, itemType);

        var typeBuilder = moduleBuilder.DefineType(
            GetCompositeHandlerFieldName(backIndexRank),
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(ValueType),
            [compositeHandlerInterfaceType, resizeHandlerInterfaceType, dataEquatorInterfaceType]);
        
        var fields = DefineConstructor(
            typeBuilder,
            dataTypeProjection.AllowsMultipleItems);
        DefineFind(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType, fields.HashTableField);
        DefineAdd(
            typeBuilder,
            dataTypeProjection,
            compositeHandlerInterfaceType,
            fields.HashTableField,
            fields.CountField);
        DefineRemove(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType, fields.HashTableField);
        DefineClear(typeBuilder, compositeHandlerInterfaceType, fields.HashTableField);

        ResizeHandlerCompilation.DefineGetHashCodeAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        ResizeHandlerCompilation.DefineGetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        ResizeHandlerCompilation.DefineSetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        DataEquatorCompilation.DefineAreDataEqualAt(typeBuilder, dataTypeProjection, dataEquatorInterfaceType);

        var type = typeBuilder.CreateType();

        return type.GetConstructor([typeof(bool)]) ?? throw new InvalidProgramException();
    }

    private static (FieldBuilder HashTableField, FieldBuilder? CountField) DefineConstructor(
        TypeBuilder typeBuilder,
        bool allowsMultipleItems)
    {
        var hashTableField = typeBuilder.DefineField(
            HashTableFieldName,
            typeof(HashEntry[]),
            FieldAttributes.InitOnly | FieldAttributes.Private);

        FieldBuilder? countField = null;
        if (allowsMultipleItems)
        {
            countField = typeBuilder.DefineField(
                CountFieldName,
                typeof(int),
                FieldAttributes.InitOnly | FieldAttributes.Private);
        }
        
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

        return (hashTableField, countField);
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
                nameof(ICompositeHandler.Find),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(SearchResult),
                [dataTypeProjection.DataTableType, dataTypeProjection.ComparerTupleType, typeof(uint), itemType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var genericFindMethod = typeof(MembershipHandling<,,,>).GetMethod(nameof(MembershipHandling.Find))!;
        
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
        FieldBuilder hashTableField,
        FieldBuilder? countField)
    {
        Type[] parameterTypes = [
            dataTypeProjection.DataTableType,
            typeof(SearchResult),
            typeof(int),
            typeof(int),
        ];

        bool allowsMultipleItems = dataTypeProjection.AllowsMultipleItems;
        var updateHandlingTypeDefinition = allowsMultipleItems ?
            typeof(MultiUpdateHandling<,>) :
            typeof(MonoUpdateHandling<,>);
        
        var updateHandlingType = updateHandlingTypeDefinition
            .MakeGenericType(dataTypeProjection.DataEntryType, typeBuilder);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(ICompositeHandler.Add),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                parameterTypes);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        Label resizeLabel = il.DefineLabel();
        Label addLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();
        
        var newCountLocal = il.DeclareLocal(typeof(int));

        if (dataTypeProjection.AllowsMultipleItems)
        {
            // In case of multiplicity if the result is SearchCase.ItemFound we do not want to change the capacity as
            // count is unchanged.
            
            // ref lastSearchResult
            il.Emit(OpCodes.Ldarga, 2);
            // (ref lastSearchResult).Case
            il.Emit(OpCodes.Call, CommonCompilation.GetCaseMethod);
            // SearchCase.ItemFound
            il.Emit(OpCodes.Ldc_I4, (int)SearchCase.ItemFound);
            // (ref lastSearchResult).Case == SearchCase.ItemFound
            il.Emit(OpCodes.Ceq);
            // (ref lastSearchResult).Case == SearchCase.ItemFound → addLabel
            il.Emit(OpCodes.Brtrue, addLabel);
            
            // If the result is not SearchCase.ItemFound then count is incremented. This may cause a resize.
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // _count
            il.Emit(OpCodes.Ldfld, countField!);
            // 1
            il.Emit(OpCodes.Ldc_I4_1);
            // _count + 1
            il.Emit(OpCodes.Add);
            // newCount = _count + 1
            il.Emit(OpCodes.Stloc, newCountLocal);
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // newCount
            il.Emit(OpCodes.Ldloc, newCountLocal);
            // _count = newCount
            il.Emit(OpCodes.Stfld, countField!);
        }
        else
        {
            // newDataCount
            il.Emit(OpCodes.Ldarg_S, (byte)4);
            // newCount = newDataCount
            il.Emit(OpCodes.Stloc, newCountLocal);
        }
        
        // Check if a resize is needed.
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        // this._hashTable.Length
        il.Emit(OpCodes.Ldlen);
        // newCount
        il.Emit(OpCodes.Ldloc, newCountLocal);
        // HashEntry.IsFullEnough(this._hashTable.Length, newCount)
        il.Emit(OpCodes.Call, typeof(HashEntry).GetMethod(nameof(HashEntry.IsFullEnough))!);
        // HashEntry.IsFullEnough(this._hashTable.Length, newCount) → resizeLabel
        il.Emit(OpCodes.Brtrue_S, resizeLabel);

        var genericAddMethod = allowsMultipleItems ?
            typeof(MultiUpdateHandling<,>).GetMethod(nameof(MultiUpdateHandling.Add))! :
            typeof(MonoUpdateHandling<,>).GetMethod(nameof(MonoUpdateHandling.Add))!;
        
        var addMethod = TypeBuilder.GetMethod(updateHandlingType, genericAddMethod);
        
        il.MarkLabel(addLabel);
        
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
        
        var genericChangeCapacityMethod = allowsMultipleItems ?
            typeof(MultiUpdateHandling<,>).GetMethod(nameof(MultiUpdateHandling.ChangeCapacity))! :
            typeof(MonoUpdateHandling<,>).GetMethod(nameof(MonoUpdateHandling.ChangeCapacity))!;
        
        var changeCapacityMethod = TypeBuilder.GetMethod(updateHandlingType, genericChangeCapacityMethod);
        
        il.MarkLabel(resizeLabel);

        if (dataTypeProjection.AllowsMultipleItems)
        {
            var genericInitializeLastBackIndexMethod = typeof(MultiUpdateHandling<,>)
                .GetMethod(nameof(MultiUpdateHandling.InitialLastBackIndex))!;
        
            var initializeLastBackIndexMethod = TypeBuilder.GetMethod(updateHandlingType, genericInitializeLastBackIndexMethod);
            
            // dataTable
            il.Emit(OpCodes.Ldarg_1);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // *this
            il.Emit(OpCodes.Ldobj, typeBuilder);
            // newDataCount
            il.Emit(OpCodes.Ldarg_S, (byte)4);
            // InitializeLastBackIndex(dataTable, *this, newDataCount)
            il.Emit(OpCodes.Call, initializeLastBackIndexMethod);
        }
        
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
                nameof(ICompositeHandler.Remove),
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
        
        var genericRemoveMethod = typeof(MonoUpdateHandling<,>).GetMethod(nameof(MonoUpdateHandling.Remove))!;
        
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
        
        var genericChangeCapacityMethod =
            typeof(MonoUpdateHandling<,>).GetMethod(nameof(MonoUpdateHandling.ChangeCapacity))!;
        
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
                nameof(ICompositeHandler.Clear),
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