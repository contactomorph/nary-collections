using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NaryMaps.Components;
using NaryMaps.Primitives;
using NaryMaps.Tools;

namespace NaryMaps.Implementation;

using CompositeInfo = (DataTypeProjection, FieldBuilder, ConstructorInfo, bool MustBeUnique, byte Rank);
using ICompositeHandler = ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>;
using ConflictHandling = ConflictHandling<
    ValueTuple,
    ValueTuple,
    ValueTuple,
    FakeNaryMap.FakeCompositeHandler,
    ValueTuple,
    ValueTuple>;

internal static class NaryMapCompilation<TSchema> where TSchema : Schema, new()
{
    private static Func<INaryMap<TSchema>>? _factory;
    
    public static Func<INaryMap<TSchema>> GenerateMapConstructor(ModuleBuilder moduleBuilder)
    {
        var schemaType = typeof(TSchema);
        if (_factory is not null)
            return _factory;

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
        
        var baseMapType = typeof(NaryMapBase<,,,,,>)
            .MakeGenericType([
                dataTupleType,
                hashTupleType,
                backIndexTupleType,
                comparerTupleType,
                compositeHandlerType,
                schemaType,
            ]);
        
        var typeBuilder = moduleBuilder.DefineType(
            CreateName(),
            TypeAttributes.Class | TypeAttributes.Sealed,
            baseMapType);

        var compositeInfo = DeclareFields(moduleBuilder, typeBuilder, schema, dataTypeDecomposition);

        DefineConstructor(typeBuilder, schemaType, compositeHandlerType, comparerTupleType, compositeInfo);
        DefineEquals(typeBuilder, dataTypeDecomposition, baseMapType);
        DefineComputeHashTuple(typeBuilder, dataTypeDecomposition, baseMapType);
        DefineFindInOtherComposites(typeBuilder, dataTypeDecomposition, baseMapType, compositeInfo);
        DefineAddToOtherComposites(typeBuilder, baseMapType, compositeInfo);
        DefineRemoveFromOtherComposites(typeBuilder, baseMapType, compositeInfo);
        DefineExtractConflictingItemsInOtherComposites(typeBuilder, dataTypeDecomposition, baseMapType, compositeInfo);
        DefineClearOtherComposites(typeBuilder, baseMapType, compositeInfo);
        DefineCreateSelection(moduleBuilder, typeBuilder, baseMapType, compositeInfo);

        var type = typeBuilder.CreateType();
        
        var ctor = type?.GetConstructors().Single() ?? throw new NullReferenceException();

        Expression[] ctorParameters = [
            Expression.Constant(schema),
            Expression.New(handlerCtor, Expression.Constant(false)),
            Expression.New(comparerTupleType.GetConstructor(), comparers)
        ];

        var factoryExpression = Expression.Lambda<Func<INaryMap<TSchema>>>(Expression.New(ctor, ctorParameters));

        var factory = factoryExpression.Compile();

        Interlocked.CompareExchange(ref _factory, factory, null);
        return _factory;
    }

    private static IReadOnlyList<CompositeInfo> DeclareFields(
        ModuleBuilder moduleBuilder,
        TypeBuilder typeBuilder,
        TSchema schema,
        DataTypeProjection dataTypeDecomposition)
    {
        var positionPerParticipants = schema
            .GetSignature()
            .Participants
            .ToDictionary(p => p.Participant, p => p.Position);
        
        List<CompositeInfo> compositeInfo = new();
        foreach (var composite in schema.GetComposites())
        {
            var indexes = composite.Participants.Select(p => positionPerParticipants[p]).ToArray();
            byte backIndexRank = (byte)(composite.Rank + 1);
            
            var otherHandlerCtor = CompositeHandlerCompilation.GenerateConstructor(
                moduleBuilder,
                dataTypeDecomposition.DataTupleType,
                indexes,
                backIndexRank, 
                dataTypeDecomposition.BackIndexMultiplicities);
            
            var fieldBuilder = typeBuilder.DefineField(
                "_compositeHandler_" + backIndexRank,
                otherHandlerCtor.DeclaringType!,
                FieldAttributes.Public);

            var otherDataTypeProjection = dataTypeDecomposition.ProjectAlong(backIndexRank, indexes);
            compositeInfo.Add((otherDataTypeProjection, fieldBuilder, otherHandlerCtor, composite.Unique, composite.Rank));
        }

        return compositeInfo;
    }

    private static string CreateName()
    {
        var sb = new StringBuilder("NaryMap_");
        Plug(sb, typeof(TSchema));
        return sb.Append($"_{Guid.NewGuid():N}").ToString();
    }

    private static void Plug(StringBuilder sb, Type type)
    {
        if (type.IsConstructedGenericType)
        {
            int n = type.Name.IndexOf('`');
            sb.Append(type.Name[..n]).Append('_');
            foreach (var typeArgument in type.GenericTypeArguments)
                Plug(sb, typeArgument);
        }
        else
        {
            sb.Append(type.Name);
        }
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

        foreach (var (_, fieldBuilder, ctor, _, _) in compositeInfo)
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

    private static void DefineEquals(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type baseCollectionType)
    {
        Type[] inputTypes = [dataTypeDecomposition.DataTupleType, dataTypeDecomposition.DataTupleType];

        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.EqualsMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(bool),
                inputTypes);
        ILGenerator il = methodBuilder.GetILGenerator();

        var falseLabel = il.DefineLabel();
        var endLabel = il.DefineLabel();

        var comparerTupleField = CommonCompilation.GetFieldInBase(
            baseCollectionType,
            FakeNaryMap.ComparerTupleFieldName);

        int i = 0;
        foreach (var dataField in dataTypeDecomposition.DataTupleType)
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._comparerTuple
            il.Emit(OpCodes.Ldfld, comparerTupleField);
            // this._comparerTuple.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, dataTypeDecomposition.ComparerTupleType[i]);
            // x
            il.Emit(OpCodes.Ldarg_1);
            // x.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, dataField);
            // y
            il.Emit(OpCodes.Ldarg_2);
            // y.Item⟨i⟩
            il.Emit(OpCodes.Ldfld, dataField);
            // EqualityComparerHandling.ComputeEquals(this._comparerTuple.Item⟨i⟩, x.Item⟨i⟩, y.Item⟨i⟩)
            il.Emit(OpCodes.Call, EqualityComparerHandling.GetEqualsMethod(dataField.FieldType));
            // !EqualityComparerHandling.ComputeEquals(…) → falseLabel
            il.Emit(OpCodes.Brfalse, falseLabel);

            ++i;
        }

        // true
        il.Emit(OpCodes.Ldc_I4_1);
        // → endLabel
        il.Emit(OpCodes.Br, endLabel);

        il.MarkLabel(falseLabel);
        // false
        il.Emit(OpCodes.Ldc_I4_0);

        il.MarkLabel(endLabel);
        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, baseCollectionType, methodBuilder, inputTypes);
    }

    private static void DefineComputeHashTuple(
        TypeBuilder typeBuilder,
        DataTypeDecomposition dataTypeDecomposition,
        Type baseMapType)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.ComputeHashTupleMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                dataTypeDecomposition.HashTupleType,
                [dataTypeDecomposition.DataTupleType]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var comparerTupleField = CommonCompilation.GetFieldInBase(
            baseMapType,
            FakeNaryMap.ComparerTupleFieldName);
        
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
        
        CommonCompilation.OverrideMethod(typeBuilder, baseMapType, methodBuilder);
    }
    
    private static void DefineFindInOtherComposites(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type baseMapType,
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        Type[] paramTypes = [
            dataTypeDecomposition.DataTupleType,
            dataTypeDecomposition.HashTupleType,
            typeof(SearchResult[]).MakeByRefType()
        ];
        
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.FindInOtherCompositesMethodName,
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
            baseMapType,
            FakeNaryMap.DataTableFieldName);
        var comparerTupleField = CommonCompilation.GetFieldInBase(
            baseMapType,
            FakeNaryMap.ComparerTupleFieldName);

        foreach (var (dataTypeProjection, handlerFieldBuilder, _, mustBeUnique, rank) in compositeInfo)
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
                nameof(ICompositeHandler.Find));
                
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
            il.Emit(OpCodes.Ldc_I4, (int)rank);
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
        }

        // false
        il.Emit(OpCodes.Ldc_I4_0);
        // → endLabel
        il.Emit(OpCodes.Br, endLabel);
        
        il.MarkLabel(alreadyInsideLabel);
        // true
        il.Emit(OpCodes.Ldc_I4_1);
        
        il.MarkLabel(endLabel);
        il.Emit(OpCodes.Ret);
        
        CommonCompilation.OverrideMethod(typeBuilder, baseMapType, methodBuilder);
    }

    private static void DefineAddToOtherComposites(
        TypeBuilder typeBuilder,
        Type baseMapType,
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.AddToOtherCompositesMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                [typeof(SearchResult[]), typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        var dataTableField = CommonCompilation.GetFieldInBase(
            baseMapType,
            NaryMapBase.DataTableFieldName);
        var countField = CommonCompilation.GetFieldInBase(
            baseMapType,
            NaryMapBase.CountFieldName);

        int i = 0;
        foreach (var (_, handlerFieldBuilder, _, _, _) in compositeInfo)
        {
            var addMethod = CommonCompilation.GetMethod(
                handlerFieldBuilder.FieldType,
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Add));
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // ref this._compositeHandler_⟨i⟩
            il.Emit(OpCodes.Ldflda, handlerFieldBuilder);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._dataTable
            il.Emit(OpCodes.Ldfld, dataTableField);
            // otherResults
            il.Emit(OpCodes.Ldarg_1);
            // i
            il.Emit(OpCodes.Ldc_I4, i);
            // otherResults[i]
            il.Emit(OpCodes.Ldelem, typeof(SearchResult));
            // candidateDataIndex
            il.Emit(OpCodes.Ldarg_2);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._count
            il.Emit(OpCodes.Ldfld, countField);
            // (ref this._compositeHandler_⟨i⟩).Add(this._dataTable, otherResults[i], candidateDataIndex, this._count)
            il.Emit(OpCodes.Call, addMethod);

            ++i;
        }
        
        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, baseMapType, methodBuilder);
    }

    private static void DefineRemoveFromOtherComposites(
        TypeBuilder typeBuilder,
        Type baseMapType,
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.RemoveFromOtherCompositesMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                [typeof(int)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        var dataTableField = CommonCompilation.GetFieldInBase(
            baseMapType,
            NaryMapBase.DataTableFieldName);
        var countField = CommonCompilation.GetFieldInBase(
            baseMapType,
            NaryMapBase.CountFieldName);

        foreach (var (_, handlerFieldBuilder, _, _, _) in compositeInfo)
        {
            var removeMethod = CommonCompilation.GetMethod(
                handlerFieldBuilder.FieldType,
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Remove));

            // this
            il.Emit(OpCodes.Ldarg_0);
            // ref this._compositeHandler_⟨i⟩
            il.Emit(OpCodes.Ldflda, handlerFieldBuilder);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._dataTable
            il.Emit(OpCodes.Ldfld, dataTableField);
            // removedDataIndex
            il.Emit(OpCodes.Ldarg_1);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._count
            il.Emit(OpCodes.Ldfld, countField);
            // (ref this._compositeHandler_⟨i⟩).Remove(this._dataTable, removedDataIndex, this._count)
            il.Emit(OpCodes.Call, removeMethod);
        }

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, baseMapType, methodBuilder);
    }

    private static void DefineExtractConflictingItemsInOtherComposites(
        TypeBuilder typeBuilder,
        DataTypeProjection dataTypeDecomposition,
        Type baseMapType,
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        Type[] paramTypes = [
            dataTypeDecomposition.DataTupleType,
            dataTypeDecomposition.HashTupleType,
            typeof(List<>).MakeGenericType(dataTypeDecomposition.DataTupleType),
        ];

        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.ExtractConflictingItemsInOtherCompositesMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                paramTypes);
        ILGenerator il = methodBuilder.GetILGenerator();

        var dataTableField = CommonCompilation.GetFieldInBase(
            baseMapType,
            FakeNaryMap.DataTableFieldName);
        var comparerTupleField = CommonCompilation.GetFieldInBase(
            baseMapType,
            FakeNaryMap.ComparerTupleFieldName);
        
        foreach (var (dataTypeProjection, handlerFieldBuilder, _, mustBeUnique, _) in compositeInfo)
        {
            if (!mustBeUnique)
                continue;

            var itemType = CommonCompilation.GetItemType(dataTypeProjection);
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._dataTable
            il.Emit(OpCodes.Ldfld, dataTableField);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._compositeHandler_⟨i⟩
            il.Emit(OpCodes.Ldfld, handlerFieldBuilder);
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
            // conflictingDataTuples
            il.Emit(OpCodes.Ldarg_3);

            var conflictHandlingType = typeof(ConflictHandling<,,,,,>)
                .MakeGenericType(
                    dataTypeProjection.DataTupleType,
                    dataTypeProjection.HashTupleType,
                    dataTypeProjection.BackIndexTupleType,
                    handlerFieldBuilder.FieldType,
                    dataTypeProjection.ComparerTupleType,
                    itemType);
            
            var extractMethod = CommonCompilation.GetStaticMethod(
                conflictHandlingType,
                nameof(ConflictHandling.ExtractDataTupleWithSameItem));

            // CompositeHandlerHandling<...>.ExtractDataTupleWithSameItem(
            //   this._dataTable, this._compositeHandler_⟨i⟩, this.ComparerTuple, ⟨hc⟩, ⟨item⟩, conflictingDataTuples)
            il.Emit(OpCodes.Call, extractMethod);
        }

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, baseMapType, methodBuilder);
    }

    private static void DefineClearOtherComposites(
        TypeBuilder typeBuilder,
        Type baseMapType,
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.ClearOtherCompositesMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(void),
                []);
        ILGenerator il = methodBuilder.GetILGenerator();

        foreach (var (_, handlerFieldBuilder, _, _, _) in compositeInfo)
        {
            var clearMethod = CommonCompilation.GetMethod(
                handlerFieldBuilder.FieldType,
                nameof(ICompositeHandler<ValueTuple, ValueTuple, ValueTuple, ValueTuple, object>.Clear));

            // this
            il.Emit(OpCodes.Ldarg_0);
            // ref this._compositeHandler_⟨i⟩
            il.Emit(OpCodes.Ldflda, handlerFieldBuilder);
            // (ref this._compositeHandler_⟨i⟩).Clear()
            il.Emit(OpCodes.Call, clearMethod);
        }

        il.Emit(OpCodes.Ret);
        
        CommonCompilation.OverrideMethod(typeBuilder, baseMapType, methodBuilder);
    }
    
    private static void DefineCreateSelection(
        ModuleBuilder moduleBuilder,
        TypeBuilder typeBuilder,
        Type baseMapType,
        IReadOnlyList<CompositeInfo> compositeInfo)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                FakeNaryMap.CreateSelectionMethodName,
                CommonCompilation.ProjectorMethodAttributes,
                typeof(object),
                [typeof(byte)]);
        ILGenerator il = methodBuilder.GetILGenerator();

        Label endLabel = il.DefineLabel();

        var labelAndRanks = compositeInfo
            .Select(info => (Label: il.DefineLabel(), Rank: info.Rank))
            .ToArray();

        foreach (var (label, rank) in labelAndRanks)
        {
            // rank
            il.Emit(OpCodes.Ldarg_1);
            // ⟨rank⟩
            il.Emit(OpCodes.Ldc_I4_S, rank);
            // rank == ⟨rank⟩ → label
            il.Emit(OpCodes.Beq, label);
        }

        // new InvalidOperationException()
        il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes)!);
        // throw new InvalidOperationException()
        il.Emit(OpCodes.Throw);

        foreach (var (proj, handlerField, _, isUnique, rank) in compositeInfo)
        {
            il.MarkLabel(labelAndRanks[rank].Label);

            var itemType = CommonCompilation.GetItemType(proj);

            var selectionBaseTypeDefinition = isUnique ?
                typeof(UniqueSearchableSelection<,,,,,>) :
                typeof(NonUniqueSearchableSelection<,,,,,>);

            var selectionBaseType = selectionBaseTypeDefinition.MakeGenericType(
                proj.DataTupleType,
                proj.DataEntryType,
                proj.ComparerTupleType,
                handlerField.FieldType,
                typeof(TSchema),
                itemType);

            var selectionConstructor = SelectionCompilation.GenerateConstructor(
                moduleBuilder,
                proj,
                selectionBaseType,
                handlerField);

            // this
            il.Emit(OpCodes.Ldarg_0);
            // new ⟨XXX⟩Selection(this)
            il.Emit(OpCodes.Newobj, selectionConstructor);

            // → endLabel
            il.Emit(OpCodes.Br, endLabel);
        }


        il.MarkLabel(endLabel);

        il.Emit(OpCodes.Ret);
        
        CommonCompilation.OverrideMethod(typeBuilder, baseMapType, methodBuilder);
    }
}
