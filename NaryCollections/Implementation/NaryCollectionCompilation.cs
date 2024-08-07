using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NaryCollections.Components;
using NaryCollections.Primitives;
using NaryCollections.Tools;

namespace NaryCollections.Implementation;

using CompositeInfo = (DataTypeProjection, FieldBuilder, ConstructorInfo, bool MustBeUnique);

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

        var backIndexMultiplicities = composites.Select(c => !c.Unique).Prepend(false).ToArray();
        var allIndexes = GetArrayOfAllIndexes(dataTupleType.Count);
        var dataTypeDecomposition = new DataTypeProjection(
            dataTupleType,
            0,
            backIndexMultiplicities,
            allIndexes);
        var hashTupleType = dataTypeDecomposition.HashTupleType;
        var backIndexTupleType = dataTypeDecomposition.BackIndexTupleType;
        var comparerTupleType = dataTypeDecomposition.ComparerTupleType;
        
        var handlerCtor = CompositeHandlerCompilation.GenerateConstructor(
            moduleBuilder,
            dataTupleType,
            allIndexes, 
            0, 
            backIndexMultiplicities);
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
        
        List<CompositeInfo> compositeInfo = new();
        foreach (var composite in composites)
        {
            var indexes = composite.Participants.Select(p => positionPerParticipants[p]).ToArray();
            var rank = (byte)(composite.Rank + 1);
            
            var otherHandlerCtor = CompositeHandlerCompilation.GenerateConstructor(
                moduleBuilder,
                dataTupleType,
                indexes,
                rank, 
                backIndexMultiplicities);
            
            var fieldBuilder = typeBuilder.DefineField(
                "_compositeHandler_" + rank,
                otherHandlerCtor.DeclaringType!,
                FieldAttributes.Private);

            var otherDataTypeProjection = dataTypeDecomposition.ProjectAlong(rank, indexes);
            compositeInfo.Add((otherDataTypeProjection, fieldBuilder, otherHandlerCtor, composite.Unique));
        }
        
        DefineConstructor(typeBuilder, schemaType, compositeHandlerType, comparerTupleType, compositeInfo);
        DefineComputeHashTuple(typeBuilder, dataTypeDecomposition, baseCollectionType);
        DefineFindInOtherComposites(typeBuilder, dataTypeDecomposition, baseCollectionType, compositeInfo);
        DefineAddToOtherComposites(typeBuilder, dataTypeDecomposition, baseCollectionType);
        DefineRemoveFromOtherComposites(typeBuilder, dataTypeDecomposition, baseCollectionType);

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
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Any,
            [schemaType, completeProjectorType, comparerTupleType]);
        var il = ctorBuilder.GetILGenerator();

        var baseCtor = typeBuilder
            .BaseType!
            .GetConstructors(CommonCompilation.BaseFlags)
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

        foreach (var (_, fieldBuilder, ctor, _) in compositeInfo)
        {
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
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                NaryCollectionBase.ComputeHashTupleMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                dataTypeDecomposition.HashTupleType,
                [dataTypeDecomposition.DataTupleType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var comparerTupleField = CommonCompilation.GetFieldInBase(
            baseCollectionType,
            NaryCollectionBase.ComparerTupleFieldName);
        
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
        
        CommonCompilation.OverrideMethod(typeBuilder, baseCollectionType, methodBuilder);
    }
    
    private static void DefineFindInOtherComposites(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type baseCollectionType,
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        Type[] paramTypes = [
            dataTypeDecomposition.DataTupleType,
            dataTypeDecomposition.HashTupleType,
            typeof(SearchResult[]).MakeByRefType()
        ];
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                NaryCollectionBase.FindInOtherCompositesMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(bool),
                paramTypes);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var equalVariable = il.DeclareLocal(typeof(bool));
        
        var alreadyInsideLabel = il.DefineLabel();
        var endLabel = il.DefineLabel();
        
        // searchResults
        il.Emit(OpCodes.Ldarg_3);
        // compositeCount
        il.Emit(OpCodes.Ldc_I4, compositeInfo.Count);
        // new SearchResult[compositeCount]
        il.Emit(OpCodes.Newarr, typeof(SearchResult));
        // searchResults = new SearchResult[compositeCount]
        il.Emit(OpCodes.Stind_Ref);

        var dataTableField = CommonCompilation.GetFieldInBase(
            baseCollectionType,
            NaryCollectionBase.DataTableFieldName);
        var comparerTupleField = CommonCompilation.GetFieldInBase(
            baseCollectionType,
            NaryCollectionBase.ComparerTupleFieldName);

        int i = 0;
        
        foreach (var (dataTypeProjection, handlerFieldBuilder, _, mustBeUnique) in compositeInfo)
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // ref this._compositeHandler_⟨i⟩
            il.Emit(OpCodes.Ldflda, handlerFieldBuilder);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._dataTable
            il.Emit(OpCodes.Ldfld, dataTableField);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this.ComparerTuple
            il.Emit(OpCodes.Ldfld, comparerTupleField);
            
            if (1 < dataTypeProjection.DataProjectionMapping.Count)
            {
                var hashMapping = dataTypeProjection.HashProjectionMapping;
                
                foreach (var correspondence in hashMapping)
                {
                    // hashTuple
                    il.Emit(OpCodes.Ldarg_2);
                    // hashTuple.Item⟨j⟩
                    il.Emit(OpCodes.Ldfld, correspondence.InputField);
                }
                
                // new ValueTuple<…>(…)
                il.Emit(OpCodes.Newobj, hashMapping.OutputType.GetConstructor());
                // EqualityComparerHandling.ComputeTupleHashCode(new ValueTuple<…>(…))
                il.Emit(OpCodes.Call, EqualityComparerHandling.GetTupleHashCodeMethod(hashMapping.OutputType));
                
                var dataMapping = dataTypeProjection.DataProjectionMapping;
                
                foreach (var correspondence in dataMapping)
                {
                    // dataTuple
                    il.Emit(OpCodes.Ldarg_1);
                    // dataTuple.Item⟨j⟩
                    il.Emit(OpCodes.Ldfld, correspondence.InputField);
                }
                // new ValueTuple<…>(…)
                il.Emit(OpCodes.Newobj, dataMapping.OutputType.GetConstructor());
            }
            else
            {
                // hashTuple
                il.Emit(OpCodes.Ldarg_2);
                // hashTuple.Item⟨j⟩
                il.Emit(OpCodes.Ldfld, dataTypeProjection.HashProjectionMapping[0].InputField);
                // dataTuple
                il.Emit(OpCodes.Ldarg_1);
                // dataTuple.Item⟨j⟩
                il.Emit(OpCodes.Ldfld, dataTypeProjection.DataProjectionMapping[0].InputField);
            }

            var findMethod = CommonCompilation.GetMethod(
                handlerFieldBuilder.FieldType,
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Find));
                
            var resultLocal = il.DeclareLocal(typeof(SearchResult));

            // (ref _compositeHandler_⟨i⟩).Find(this._dataTable, this.ComparerTuple, ⟨hc⟩, ⟨item⟩)
            il.Emit(OpCodes.Call, findMethod);
            // result = (ref _compositeHandler_⟨i⟩).Find(this._dataTable, this.ComparerTuple, ⟨hc⟩, ⟨item⟩)
            il.Emit(OpCodes.Stloc, resultLocal);
            
            // searchResults
            il.Emit(OpCodes.Ldarg_3);
            // *searchResults
            il.Emit(OpCodes.Ldind_Ref);
            // i
            il.Emit(OpCodes.Ldc_I4, i);
            // result
            il.Emit(OpCodes.Ldloc, resultLocal);
            // (*searchResults)[i] = result
            il.Emit(OpCodes.Stelem, typeof(SearchResult));

            if (mustBeUnique)
            {
                // ref result
                il.Emit(OpCodes.Ldloca, resultLocal);
                // (ref result).Case
                il.Emit(OpCodes.Call, CommonCompilation.GetCaseMethod);
                // SearchCase.ItemFound
                il.Emit(OpCodes.Ldc_I4, (int)SearchCase.ItemFound);
                // (ref result).Case == SearchCase.ItemFound
                il.Emit(OpCodes.Ceq);
                // equal = (ref result).Case == SearchCase.ItemFound
                il.Emit(OpCodes.Stloc, equalVariable);
                // equal
                il.Emit(OpCodes.Ldloc, equalVariable);
                // equal → alreadyInsideLabel
                il.Emit(OpCodes.Brtrue, alreadyInsideLabel);
            }

            ++i;
        }

        // false
        il.Emit(OpCodes.Ldc_I4_0);
        // → endLabel
        il.Emit(OpCodes.Br_S, endLabel);
        
        il.MarkLabel(alreadyInsideLabel);
        // true
        il.Emit(OpCodes.Ldc_I4_1);
        
        il.MarkLabel(endLabel);
        il.Emit(OpCodes.Ret);
        
        CommonCompilation.OverrideMethod(typeBuilder, baseCollectionType, methodBuilder);
    }

    private static void DefineAddToOtherComposites(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type baseCollectionType)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                NaryCollectionBase.AddToOtherCompositesMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                [typeof(SearchResult[]), typeof(int), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, baseCollectionType, methodBuilder);
    }
    
    private static void DefineRemoveFromOtherComposites(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type baseCollectionType)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                NaryCollectionBase.RemoveFromOtherCompositesMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                [typeof(SearchResult[]), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        il.Emit(OpCodes.Ret);
        
        CommonCompilation.OverrideMethod(typeBuilder, baseCollectionType, methodBuilder);
    }
}
