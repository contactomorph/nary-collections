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
        DefineComputeHashCode(typeBuilder, dataTypeProjection, projectorInterfaceType, comparerFields);

        Type resizeHandlerInterfaceType = projectorInterfaceType
            .GetInterfaces()
            .Single(i => i.Name.StartsWith(nameof(IResizeHandler<ValueTuple>)));
        Type dataEquatorInterfaceType = projectorInterfaceType
            .GetInterfaces()
            .Single(i => i.Name.StartsWith(nameof(IDataEquator<ValueTuple, ValueTuple, object>)));
        
        UpdateHandlerCompilation.DefineGetHashCodeAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        UpdateHandlerCompilation.DefineGetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        UpdateHandlerCompilation.DefineSetBackIndexAt(typeBuilder, dataTypeProjection, resizeHandlerInterfaceType);
        
        DataEquatorCompilation.DefineAreDataEqualAt(typeBuilder, dataTypeProjection, dataEquatorInterfaceType);

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
    
    private static void DefineComputeHashCode(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeProjection,
        Type projectorInterfaceType,
        IReadOnlyList<FieldBuilder> comparerFields)
    {
        const string methodName = nameof(IDataProjector<object, object, object>.ComputeHashCode);
        
        var itemType = CommonCompilation.GetItemType(dataTypeProjection);
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
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
