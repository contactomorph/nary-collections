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
        const string methodName = nameof(IItemHasher<ValueTuple, object>.ComputeHashCode);
        
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(uint),
                [dataTypeProjection.ComparerTupleType, itemType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var dataMapping = dataTypeProjection.DataProjectionMapping;
        if (dataMapping.Count == 1)
        {
            var getHashCode = EqualityComparerHandling.GetItemHashCodeMethod(itemType);

            byte b = dataMapping[0].Index;
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
            int j = 0;
            foreach (var (b, _) in dataMapping)
            {
                var itemField = dataMapping.OutputType[j];
                var comparerField = dataTypeProjection.ComparerTupleType[b];
            
                // comparerTuple
                il.Emit(OpCodes.Ldarg_1);
                // comparerTuple.Item⟨b⟩
                il.Emit(OpCodes.Ldfld, comparerField);
                // item
                il.Emit(OpCodes.Ldarg_2);
                // item.Item⟨j⟩
                il.Emit(OpCodes.Ldfld, itemField);
                // EqualityComparerHandling.Compute⟨Struct|Ref⟩HashCode(comparerTuple.Item⟨b⟩, item.Item⟨j⟩)
                il.Emit(OpCodes.Call, EqualityComparerHandling.GetItemHashCodeMethod(itemField.FieldType));

                ++j;
            }
            
            var itemHashTupleType = ValueTupleType.FromRepeatedComponent<uint>(dataMapping.Count);
            
            // new ValueTuple<…>(…)
            il.Emit(OpCodes.Newobj, itemHashTupleType.GetConstructor());
            // EqualityComparerHandling.ComputeTupleHashCode(new ValueTuple<…>(…))
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetTupleHashCodeMethod(itemHashTupleType));
        }
        
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, itemHasherInterfaceType.GetMethod(methodName)!);
    }

}