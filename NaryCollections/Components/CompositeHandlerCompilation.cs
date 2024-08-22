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
        DefineRemove(
            typeBuilder,
            dataTypeProjection,
            compositeHandlerInterfaceType,
            fields.HashTableField,
            fields.CountField);
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
        
        var genericAddMethod = allowsMultipleItems ?
            typeof(MultiUpdateHandling<,>).GetMethod(nameof(MultiUpdateHandling.Add))! :
            typeof(MonoUpdateHandling<,>).GetMethod(nameof(MonoUpdateHandling.Add))!;
        
        var addMethod = TypeBuilder.GetMethod(updateHandlingType, genericAddMethod);
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // ref this._hashTable
        il.Emit(OpCodes.Ldflda, hashTableField);
        
        if (dataTypeProjection.AllowsMultipleItems)
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // ref this._count
            il.Emit(OpCodes.Ldflda, countField!);
        }

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
        // newDataCount
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        // Add(ref this._hashTable, …, newDataCount)
        il.Emit(OpCodes.Call, addMethod);
        
        il.Emit(OpCodes.Ret);
        
        CommonCompilation.OverrideMethod(typeBuilder, compositeHandlerInterfaceType, methodBuilder);
    }

    private static void DefineRemove(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type compositeHandlerInterfaceType,
        FieldBuilder hashTableField,
        FieldBuilder? countField)
    {
        var updateHandlingTypeDefinition = typeof(MonoUpdateHandling<,>);
        var updateHandlingType = updateHandlingTypeDefinition
            .MakeGenericType(dataTypeProjection.DataEntryType, typeBuilder);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(ICompositeHandler.Remove),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                [dataTypeProjection.DataTableType, typeof(int), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var genericRemoveMethod = typeof(MonoUpdateHandling<,>).GetMethod(nameof(MonoUpdateHandling.Remove))!;
        
        var removeMethod = TypeBuilder.GetMethod(updateHandlingType, genericRemoveMethod);
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // ref this._hashTable
        il.Emit(OpCodes.Ldflda, hashTableField);
        // currentDataCount
        il.Emit(OpCodes.Ldarg_3);
        // dataTable
        il.Emit(OpCodes.Ldarg_1);
        // this
        il.Emit(OpCodes.Ldarg_0);
        // *this
        il.Emit(OpCodes.Ldobj, typeBuilder);
        // removedDataIndex
        il.Emit(OpCodes.Ldarg_2);
        // Remove(ref this._hashTable, …, dataTable, *this, removedDataIndex)
        il.Emit(OpCodes.Call, removeMethod);

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