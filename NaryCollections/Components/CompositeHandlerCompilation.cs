using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Primitives;

namespace NaryCollections.Components;

public static class CompositeHandlerCompilation
{
    private static readonly MethodAttributes ProjectorMethodAttributes =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final;
    
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
        
        var itemType = GetItemType(dataTypeProjection);
        
        Type compositeHandlerInterfaceType = typeof(ICompositeHandler<,,,,>)
            .MakeGenericType(
                dataTypeProjection.DataTupleType,
                dataTypeProjection.HashTupleType,
                dataTypeProjection.BackIndexTupleType,
                dataTypeProjection.ComparerTupleTypes,
                itemType);

        var typeBuilder = moduleBuilder.DefineType(
            $"CompositeHandler_{backIndexRank}",
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(ValueType),
            [compositeHandlerInterfaceType]);
        
        DefineConstructor(typeBuilder);
        DefineContains(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType);
        DefineAdd(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType);
        DefineRemove(typeBuilder, dataTypeProjection, compositeHandlerInterfaceType);

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
        
        var itemType = GetItemType(dataTypeProjection);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(SearchResult),
                [dataTypeProjection.DataTableType, dataTypeProjection.ComparerTupleTypes, typeof(uint), itemType]);
        ILGenerator il = methodBuilder.GetILGenerator();

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Call, typeof(SearchResult).GetMethod(nameof(SearchResult.CreateForItemFound))!);
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, compositeHandlerInterfaceType.GetMethod(methodName)!);
    }
    
    private static void DefineAdd(TypeBuilder typeBuilder, DataTypeProjection dataTypeProjection, Type compositeHandlerInterfaceType)
    {
        const string methodName = nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Add);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(void),
                [dataTypeProjection.DataTableType, dataTypeProjection.ComparerTupleTypes, typeof(SearchResult), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, compositeHandlerInterfaceType.GetMethod(methodName)!);
    }

    private static void DefineRemove(TypeBuilder typeBuilder, DataTypeProjection dataTypeProjection, Type compositeHandlerInterfaceType)
    {
        const string methodName = nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Remove);
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                typeof(void),
                [dataTypeProjection.DataTableType, dataTypeProjection.ComparerTupleTypes, typeof(int), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, compositeHandlerInterfaceType.GetMethod(methodName)!);
    }

    private static Type GetItemType(DataTypeProjection dataTypeProjection)
    {
        var dataMappingOutput = dataTypeProjection.DataProjectionMapping.OutputType;
        return dataMappingOutput.Count == 1 ? dataMappingOutput[0].FieldType : dataMappingOutput;
    }
}