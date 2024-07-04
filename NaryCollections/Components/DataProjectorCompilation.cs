using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Primitives;
using NaryCollections.Tools;

namespace NaryCollections.Components;

public static class DataProjectorCompilation
{
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
        
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        
        Type projectorInterfaceType = typeof(IDataProjector<,,>)
            .MakeGenericType(dataTypeProjection.DataEntryType, dataTypeProjection.ComparerTupleType, itemType);

        var typeBuilder = moduleBuilder.DefineType(
            $"DataProjector_{backIndexRank}",
            TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(ValueType),
            [projectorInterfaceType]);
        
        var comparerFields = DefineConstructor(typeBuilder, dataTypeProjection.ComparerTypes, projectionIndexes);

        Type resizeHandlerInterfaceType = projectorInterfaceType
            .GetInterfaces()
            .Single(i => i.Name.StartsWith(nameof(IResizeHandler<ValueTuple>)));
        Type dataEquatorInterfaceType = projectorInterfaceType
            .GetInterfaces()
            .Single(i => i.Name.StartsWith(nameof(IDataEquator<ValueTuple, ValueTuple, object>)));
        Type itemHasherInterfaceType = projectorInterfaceType
            .GetInterfaces()
            .Single(i => i.Name.StartsWith(nameof(IItemHasher<ValueTuple, object>)));
        
        ResizeHandlerCompilation.DefineGetHashCodeAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        ResizeHandlerCompilation.DefineGetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        ResizeHandlerCompilation.DefineSetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        
        DataEquatorCompilation.DefineAreDataEqualAt(typeBuilder, dataTypeProjection, dataEquatorInterfaceType);
        ItemHasherCompilation.DefineComputeHashCode(typeBuilder, dataTypeProjection, itemHasherInterfaceType);

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

    private static string GetComparerFieldName(int i) => $"_comparer{i}";
}
