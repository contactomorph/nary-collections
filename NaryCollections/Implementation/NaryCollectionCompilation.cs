using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tools;

namespace NaryCollections.Implementation;

internal static class NaryCollectionCompilation<TSchema> where TSchema : Schema, new()
{
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
        byte backIndexCount = (byte)(composites.Length + 1);
        var allIndexes = GetArrayOfAllIndexes(dataTupleType.Count);
        var dataTypeDecomposition = new DataTypeProjection(dataTupleType, 0, backIndexCount, allIndexes);
        var hashTupleType = dataTypeDecomposition.HashTupleType;
        var backIndexTupleType = dataTypeDecomposition.BackIndexTupleType;
        var comparerTupleType = dataTypeDecomposition.ComparerTupleType;
        
        var handlerCtor = CompositeHandlerCompilation.GenerateConstructor(
            moduleBuilder,
            dataTupleType,
            allIndexes, 
            0, 
            backIndexCount);
        var compositeHandlerType = handlerCtor.DeclaringType!;
        
        var comparers = GetEqualityComparers(dataTypeDecomposition.DataTupleType)
            .Select(Expression.Constant)
            .ToArray<Expression>();
        
        var baseCollectionType = typeof(NaryCollectionBase<,,,,,>)
            .MakeGenericType([
                dataTupleType,
                hashTupleType,
                backIndexTupleType,
                comparerTupleType,
                compositeHandlerType,
                schemaType,
            ]);
        
        var typeBuilder = moduleBuilder.DefineType(
            "NaryCollection",
            TypeAttributes.Class | TypeAttributes.Sealed,
            baseCollectionType);

        var positionPerParticipants = schema
            .GetSignature()
            .Participants
            .ToDictionary(p => p.Participant, p => p.Position);
        
        List<FieldBuilder> handlerFieldBuilders = new();
        foreach (var composite in composites)
        {
            var indexes = composite.Participants.Select(p => positionPerParticipants[p]).ToArray();
            var rank = (byte)(composite.Rank + 1);
            
            var otherHandlerCtor = CompositeHandlerCompilation.GenerateConstructor(
                moduleBuilder,
                dataTupleType,
                indexes,
                rank, 
                backIndexCount);
            var otherCompositeHandlerType = otherHandlerCtor.DeclaringType!;
            
            var fieldBuilder = typeBuilder
                .DefineField("_compositeHandler_" + rank, otherCompositeHandlerType, FieldAttributes.Private);
            handlerFieldBuilders.Add(fieldBuilder);
        }
        
        DefineConstructor(typeBuilder, schemaType, compositeHandlerType, comparerTupleType, handlerFieldBuilders);
        DefineComputeHashTuple(typeBuilder, dataTypeDecomposition, baseCollectionType);

        var type = typeBuilder.CreateType();
        
        var ctor = type.GetConstructors().Single();

        Expression[] ctorParameters = [
            Expression.Constant(schema),
            Expression.New(handlerCtor, Expression.Constant(false)),
            Expression.New(comparerTupleType.GetConstructor(), comparers)
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

    private static void DefineConstructor(
        TypeBuilder typeBuilder,
        Type schemaType,
        Type completeProjectorType,
        ValueTupleType comparerTupleType,
        IReadOnlyList<FieldBuilder> handlerFieldBuilders)
    {
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Any,
            [schemaType, completeProjectorType, comparerTupleType]);
        var il = ctorBuilder.GetILGenerator();

        var baseCtor = typeBuilder
            .BaseType!
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // schema
        il.Emit(OpCodes.Ldarg_1);
        // completeProjector
        il.Emit(OpCodes.Ldarg_2);
        // comparerTuple
        il.Emit(OpCodes.Ldarg_3);
        // base(schema, completeProjector, comparerTuple)
        il.Emit(OpCodes.Call, baseCtor);

        foreach (var fieldBuilder in handlerFieldBuilders)
        {
            var ctor = fieldBuilder.FieldType.GetConstructor([typeof(bool)])!;
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // false
            il.Emit(OpCodes.Ldc_I4_0);
            // new CompositeHandler(false)
            il.Emit(OpCodes.Newobj, ctor);
            // this._compositeHandler_⟨i⟩ = new CompositeHandler(false)
            il.Emit(OpCodes.Stfld, fieldBuilder);
        }
        
        il.Emit(OpCodes.Ret);
    }

    private static void DefineComputeHashTuple(
        TypeBuilder typeBuilder,
        DataTypeDecomposition dataTypeDecomposition,
        Type baseCollectionType)
    {
        const string methodName = "ComputeHashTuple";
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                methodName,
                CommonCompilation.ProjectorMethodAttributes,
                dataTypeDecomposition.HashTupleType,
                [dataTypeDecomposition.DataTupleType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var comparerTupleField = baseCollectionType
             .GetField(
                "ComparerTuple",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new MissingFieldException();
        
        int i = 0;
        foreach (var dataField in dataTypeDecomposition.DataTupleType)
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._comparerTuple
            il.Emit(OpCodes.Ldfld, comparerTupleField);
            // this._comparerTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, dataTypeDecomposition.ComparerTupleType[i]);
            // dataTuple
            il.Emit(OpCodes.Ldarg_1);
            // dataTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, dataField);
            // EqualityComparerHandling.Compute⟨Struct|Ref⟩HashCode(this._comparerTuple.Item⟨i⟩, dataTuple.Item⟨i⟩)
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetItemHashCodeMethod(dataField.FieldType));
        
            ++i;
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