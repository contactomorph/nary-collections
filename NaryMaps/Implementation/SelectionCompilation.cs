using System.Reflection;
using System.Reflection.Emit;
using NaryMaps.Components;
using NaryMaps.Fakes;
using NaryMaps.Primitives;
using NaryMaps.Tools;

namespace NaryMaps.Implementation;

using Selection = SelectionBase<ValueTuple, ValueTuple, ValueTuple, FakeHashTableProvider, object>;

public static class SelectionCompilation
{
    private static string GetSelectionTypeName(byte backIndexRank) => $"Selection_{backIndexRank}";
    
    internal static ConstructorInfo GenerateConstructor(
        ModuleBuilder moduleBuilder,
        DataTypeProjection dataTypeProjection,
        Type selectionBaseType,
        FieldInfo handlerField)
    {
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);

        var mapType = typeof(NaryMapCore<,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, dataTypeProjection.ComparerTupleType);
        
        if (!mapType.IsAssignableFrom(handlerField.DeclaringType))
            throw new ArgumentException(nameof(handlerField));
        
        Type selectionMostBaseType = typeof(SelectionBase<,,,,>)
            .MakeGenericType(
                dataTypeProjection.DataTupleType,
                dataTypeProjection.DataEntryType,
                dataTypeProjection.ComparerTupleType,
                handlerField.FieldType,
                itemType);
        
        if (!selectionMostBaseType.IsAssignableFrom(selectionBaseType))
            throw new ArgumentException(nameof(selectionBaseType));
        
        var typeBuilder = moduleBuilder.DefineType(
            GetSelectionTypeName(dataTypeProjection.BackIndexRank),
            TypeAttributes.Class | TypeAttributes.Sealed,
            selectionBaseType);
        
        DefineConstructor(typeBuilder, dataTypeProjection);
        DefineGetHandler(typeBuilder, selectionBaseType, handlerField);
        DefineGetItem(typeBuilder, dataTypeProjection, selectionBaseType);
        DefineGetDataTuple(typeBuilder, dataTypeProjection, selectionBaseType);
        DefineComputeHashCode(typeBuilder, dataTypeProjection, selectionBaseType);

        var type = typeBuilder.CreateType();

        return type.GetConstructors().Single();
    }
    
    private static void DefineConstructor(
        TypeBuilder typeBuilder,
        DataTypeDecomposition dataTypeDecomposition)
    {
        var mapCoreType = typeof(NaryMapCore<,>)
            .MakeGenericType(dataTypeDecomposition.DataEntryType, dataTypeDecomposition.ComparerTupleType);
        
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Any,
            [mapCoreType]);
        var il = ctorBuilder.GetILGenerator();

        var baseCtor = typeBuilder
            .BaseType!
            .GetConstructors(CommonCompilation.BaseFlags)
            .Single();
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // map
        il.Emit(OpCodes.Ldarg_1);
        // base(map)
        il.Emit(OpCodes.Call, baseCtor);
        
        il.Emit(OpCodes.Ret);
    }

    private static void DefineGetHandler(
        TypeBuilder typeBuilder,
        Type selectionBaseType,
        FieldInfo handlerField)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(Selection.GetHandler),
                CommonCompilation.ProjectorMethodAttributes,
                handlerField.FieldType,
                []);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var mapField = CommonCompilation.GetFieldInBase(selectionBaseType, "_map");
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._map
        il.Emit(OpCodes.Ldfld, mapField);
        // this._map._compositeHandler
        il.Emit(OpCodes.Ldfld, handlerField);

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, selectionBaseType, methodBuilder);
    }

    private static void DefineGetItem(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type selectionBaseType)
    {
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(Selection.GetItem),
                CommonCompilation.ProjectorMethodAttributes,
                itemType,
                [dataTypeProjection.DataEntryType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var dataTupleField = CommonCompilation.GetFieldInBase(
            dataTypeProjection.DataEntryType,
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple));
        
        var dataMapping = dataTypeProjection.DataProjectionMapping;

        foreach (var (_, _, _, _, inputField) in dataMapping)
        {
            // dataEntry
            il.Emit(OpCodes.Ldarg_1);
            // dataEntry.DataTuple
            il.Emit(OpCodes.Ldfld, dataTupleField);
            // dataEntry.DataTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, inputField);
        }
        
        if (1 < dataMapping.Count)
        {
            // new ValueTuple<…>(…)
            il.Emit(OpCodes.Newobj, dataMapping.OutputType.GetConstructor());
        }

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, selectionBaseType, methodBuilder);
    }

    private static void DefineGetDataTuple(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type selectionBaseType)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(Selection.GetDataTuple),
                CommonCompilation.ProjectorMethodAttributes,
                dataTypeProjection.DataTupleType,
                [dataTypeProjection.DataEntryType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var dataTupleField = CommonCompilation.GetFieldInBase(
            dataTypeProjection.DataEntryType,
            nameof(DataEntry<ValueTuple, ValueTuple, ValueTuple>.DataTuple));
        
        // dataEntry
        il.Emit(OpCodes.Ldarg_1);
        // dataEntry.DataTuple
        il.Emit(OpCodes.Ldfld, dataTupleField);

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, selectionBaseType, methodBuilder);
    }

    private static void DefineComputeHashCode(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type selectionBaseType)
    {
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(Selection.ComputeHashCode),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(uint),
                [dataTypeProjection.ComparerTupleType, itemType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var dataMapping = dataTypeProjection.DataProjectionMapping;
        if (dataMapping.Count == 1)
        {
            var getHashCode = EqualityComparerHandling.GetItemHashCodeMethod(itemType);

            byte b = dataMapping[0].InputIndex;
            var comparerField = dataTypeProjection.ComparerTupleType[b];
            
            // comparerTuple
            il.Emit(OpCodes.Ldarg_1);
            // comparerTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, comparerField);
            // item
            il.Emit(OpCodes.Ldarg_2);
            // EqualityComparerHandling.Compute⟨Struct|Ref⟩HashCode(comparerTuple.Item⟨i⟩, item)
            il.Emit(OpCodes.Call, getHashCode);
        }
        else
        {
            foreach (var (type, _, outputField, b, _) in dataMapping)
            {
                var comparerField = dataTypeProjection.ComparerTupleType[b];
            
                // comparerTuple
                il.Emit(OpCodes.Ldarg_1);
                // comparerTuple.Item⟨b⟩
                il.Emit(OpCodes.Ldfld, comparerField);
                // item
                il.Emit(OpCodes.Ldarg_2);
                // item.Item⟨j⟩
                il.Emit(OpCodes.Ldfld, outputField);
                // EqualityComparerHandling.Compute⟨Struct|Ref⟩HashCode(comparerTuple.Item⟨b⟩, item.Item⟨j⟩)
                il.Emit(OpCodes.Call, EqualityComparerHandling.GetItemHashCodeMethod(type));
            }
            
            var itemHashTupleType = ValueTupleType.FromRepeatedComponent<uint>(dataMapping.Count);
            
            // new ValueTuple<…>(…)
            il.Emit(OpCodes.Newobj, itemHashTupleType.GetConstructor());
            // EqualityComparerHandling.ComputeTupleHashCode(new ValueTuple<…>(…))
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetTupleHashCodeMethod(itemHashTupleType));
        }
        
        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, selectionBaseType, methodBuilder);
    }
}