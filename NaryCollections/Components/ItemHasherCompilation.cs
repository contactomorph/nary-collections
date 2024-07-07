using System.Reflection.Emit;
using NaryCollections.Primitives;
using NaryCollections.Tools;

namespace NaryCollections.Components;

internal static class ItemHasherCompilation
{
    public static void DefineComputeHashCode(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type itemHasherInterfaceType)
    {
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(IItemHasher<ValueTuple, object>.ComputeHashCode),
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

        CommonCompilation.OverrideMethod(typeBuilder, itemHasherInterfaceType, methodBuilder);
    }

}