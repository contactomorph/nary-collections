using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Details;
using NaryCollections.Tools;

namespace NaryCollections.Implementation;

internal static class NaryCollectionCompilation<TSchema> where TSchema : Schema, new()
{
    private static readonly MethodAttributes ProjectorMethodAttributes =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final;
    
    private static Builder? _builder;

    public sealed record Builder(ConstructorInfo Ctor, Func<INaryCollection<TSchema>> Factory);
    
    public static Builder GenerateCollectionConstructor(ModuleBuilder moduleBuilder)
    {
        var schemaType = typeof(TSchema);
        if (_builder is not null)
            return _builder;

        var schema = new TSchema();

        var dataTupleType = ValueTupleType.From(schema.DataTupleType) ?? throw new InvalidProgramException();
        var composites = schema.GetComposites();
        var allIndexes = GetArrayOfAllIndexes(dataTupleType.Count);
        var dataTypeDecomposition = new DataTypeProjection(dataTupleType, 0, (byte)composites.Length, allIndexes);
        var hashTupleType = dataTypeDecomposition.HashTupleType;
        var backIndexTupleType = dataTypeDecomposition.BackIndexTupleType;
        
        var completeProjectorCtor = DataProjectorCompilation.GenerateProjectorConstructor(
            moduleBuilder,
            dataTupleType,
            allIndexes, 
            0, 
            (byte)composites.Length);
        
        var comparers = GetEqualityComparers(dataTypeDecomposition.DataTupleType)
            .Select(Expression.Constant)
            .ToArray<Expression>();
        
        var baseCollectionType = typeof(NaryCollectionBase<,,,>)
            .MakeGenericType([dataTupleType, hashTupleType, backIndexTupleType, schemaType]);
        
        var typeBuilder = moduleBuilder.DefineType(
            "NaryCollection",
            TypeAttributes.Class | TypeAttributes.Sealed,
            baseCollectionType);

        var comparerFields = DefineConstructor(
            typeBuilder,
            schemaType,
            completeProjectorCtor.DeclaringType!,
            dataTypeDecomposition.ComparerTypes);
        DefineComputeHashTuple(typeBuilder, dataTypeDecomposition, baseCollectionType, comparerFields);

        var type = typeBuilder.CreateType();
        
        var ctor = type.GetConstructors().Single();

        Expression[] ctorParameters = [
            Expression.Constant(schema),
            Expression.New(completeProjectorCtor, comparers),
            ..comparers
        ];

        var factoryExpression = Expression.Lambda<Func<INaryCollection<TSchema>>>(Expression.New(ctor, ctorParameters));

        var builder = new Builder(ctor, factoryExpression.Compile());

        Interlocked.CompareExchange(ref _builder, builder, null);
        return _builder;
    }

    private static object[] GetEqualityComparers(ValueTupleType dataTupleType)
    {
        object CreateDefaultEqualityComparer(Type type)
        {
            return typeof(EqualityComparer<>)
                .MakeGenericType(type)
                .GetProperty(nameof(EqualityComparer<object>.Default))!
                .GetValue(null)!;
        }

        return dataTupleType.Select(f => CreateDefaultEqualityComparer(f.FieldType)).ToArray();
    }

    private static byte[] GetArrayOfAllIndexes(int length)
    {
        return Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
    }

    private static List<FieldBuilder> DefineConstructor(
        TypeBuilder typeBuilder,
        Type schemaType,
        Type completeProjectorType,
        IReadOnlyList<Type> comparerTypes)
    {
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Any,
            [schemaType, completeProjectorType, ..comparerTypes]);
        var il = ctorBuilder.GetILGenerator();

        var baseCtor = typeBuilder
            .BaseType!
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();
        
        List<FieldBuilder> comparerFields = new();
        int j = 0;
        foreach (var comparerType in comparerTypes)
        {
            var comparerField = typeBuilder.DefineField(
                "_comparer" + j,
                comparerType,
                FieldAttributes.InitOnly);
            comparerFields.Add(comparerField);
            ++j;
        }
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, baseCtor);

        for (byte b = 0; b < comparerFields.Count; ++b)
        {
            var comparerField = comparerFields[b];
            // this
            il.Emit(OpCodes.Ldarg_0);
            // comparer⟨b⟩
            il.Emit(OpCodes.Ldarg_S, b + 3);
            // this._comparer⟨b⟩ = comparer⟨b⟩
            il.Emit(OpCodes.Stfld, comparerField);
        }
        
        il.Emit(OpCodes.Ret);

        return comparerFields;
    }

    private static void DefineComputeHashTuple(
        TypeBuilder typeBuilder,
        DataTypeDecomposition dataTypeDecomposition,
        Type baseCollectionType,
        IReadOnlyList<FieldBuilder> comparerFields)
    {
        const string methodName = "ComputeHashTuple";
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                ProjectorMethodAttributes,
                dataTypeDecomposition.HashTupleType,
                [dataTypeDecomposition.DataTupleType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        int j = 0;
        foreach (var dataField in dataTypeDecomposition.DataTupleType)
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._comparer⟨j⟩
            il.Emit(OpCodes.Ldfld, comparerFields[j]);
            // dataTuple
            il.Emit(OpCodes.Ldarg_1);
            // dataTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, dataField);
            // EqualityComparerHandling.Compute⟨Struct|Ref⟩HashCode(this._comparer⟨j⟩, dataTuple.Item⟨i⟩)
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetItemHashCodeMethod(dataField.FieldType));
        
            ++j;
        }
        
        var hashTupleType = dataTypeDecomposition.HashTupleType;
            
        // new ValueTuple<…>(…)
        il.Emit(OpCodes.Newobj, hashTupleType.GetConstructor());
        
        il.Emit(OpCodes.Ret);

        var method = baseCollectionType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
                     throw new InvalidProgramException();
        
        typeBuilder.DefineMethodOverride(methodBuilder, method);
    }
}