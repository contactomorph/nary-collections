using System.Reflection.Emit;
using NaryMaps.Primitives;

namespace NaryMaps.Components;

public static class HashTableProviderCompilation
{
    internal static void DefineGetHashEntryCount(TypeBuilder typeBuilder, FieldBuilder? countField)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(IHashTableProvider.GetHashEntryCount),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(int),
                []);
        ILGenerator il = methodBuilder.GetILGenerator();

        if (countField is not null)
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this._count
            il.Emit(OpCodes.Ldfld, countField);
        }
        else
        {
            // -1
            il.Emit(OpCodes.Ldc_I4, -1);
        }

        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, typeof(IHashTableProvider), methodBuilder);
    }

    internal static void DefineGetHashTable(TypeBuilder typeBuilder, FieldBuilder hashTableField)
    {
        MethodBuilder methodBuilder = typeBuilder
            .DefineMethod(
                nameof(IHashTableProvider.GetHashTable),
                CommonCompilation.ProjectorMethodAttributes,
                typeof(HashEntry[]),
                []);
        ILGenerator il = methodBuilder.GetILGenerator();
        
        // this
        il.Emit(OpCodes.Ldarg_0);
        // this._hashTable
        il.Emit(OpCodes.Ldfld, hashTableField);
        
        il.Emit(OpCodes.Ret);

        CommonCompilation.OverrideMethod(typeBuilder, typeof(IHashTableProvider), methodBuilder);
    }
}