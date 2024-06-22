using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Details;
using NaryCollections.Tools;

namespace NaryCollections.Implementation;

internal static class NaryCollectionCompilation<TSchema> where TSchema : Schema, new()
{
    private static Builder? _builder;

    public sealed record Builder(ConstructorInfo Ctor, Func<INaryCollection<TSchema>> Factory);
    
    public static Builder GenerateCollectionConstructor(
        ModuleBuilder moduleBuilder)
    {
        var schemaType = typeof(TSchema);
        if (_builder is not null)
            return _builder;

        var schema = new TSchema();

        var argTupleType = ValueTupleType.From(schema.ArgTupleType) ?? throw new InvalidProgramException();
        var composites = schema.GetComposites();
        var allIndexes = GetArrayOfAllIndexes(argTupleType.Count);
        var dataTypeDecomposition = new DataTypeProjection(argTupleType, 0, (byte)composites.Length, allIndexes);
        var hashTupleType = dataTypeDecomposition.HashTupleType;
        var backIndexTupleType = dataTypeDecomposition.BackIndexTupleType;
        
        var completeProjectorCtor = DataProjectorCompilation.GenerateProjectorConstructor(
            moduleBuilder,
            argTupleType,
            allIndexes, 
            0, 
            (byte)composites.Length);
        
        var comparers = GetEqualityComparers(dataTypeDecomposition.DataTupleType)
            .Select(Expression.Constant)
            .ToArray<Expression>();
        
        var baseCollectionType = typeof(NaryCollectionBase<,,,>)
            .MakeGenericType([argTupleType, hashTupleType, backIndexTupleType, schemaType]);
        
        var typeBuilder = moduleBuilder.DefineType(
            "NaryCollection",
            TypeAttributes.Class | TypeAttributes.Sealed,
            baseCollectionType);

        DefineConstructor(typeBuilder, schemaType, completeProjectorCtor.DeclaringType!);

        var type = typeBuilder.CreateType();
        
        var ctor = type.GetConstructor([schemaType, completeProjectorCtor.DeclaringType!]) ??
                   throw new InvalidProgramException();

        var factoryExpression = Expression.Lambda<Func<INaryCollection<TSchema>>>(
            Expression.New(ctor, Expression.Constant(schema), Expression.New(completeProjectorCtor, comparers)));

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

    private static void DefineConstructor(TypeBuilder typeBuilder, Type schemaType, Type completeProjectorType)
    {
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Any,
            [schemaType, completeProjectorType]);
        var il = ctorBuilder.GetILGenerator();

        var baseCtor = typeBuilder
            .BaseType!
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, baseCtor);
        il.Emit(OpCodes.Ret);
    }
}