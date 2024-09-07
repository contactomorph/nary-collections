using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using NaryMaps.Implementation;

namespace NaryMaps;

public static class NaryMap
{
    private static ModuleBuilder? _moduleBuilder;
    
    public static INaryMap<TSchema> New<TSchema>() where TSchema : Schema, new()
    {
        if (_moduleBuilder is null)
        {
            var guid = Guid.NewGuid();
            AssemblyName assembly = new AssemblyName { Name = $"nary_map_{guid:N}" };
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("nary_map_module");

            Interlocked.CompareExchange(ref _moduleBuilder, moduleBuilder, null);
        }
        var factory = NaryMapCompilation<TSchema>.GenerateMapConstructor(_moduleBuilder);
        return factory();
    }

    public static ISet<TDataTuple> AsSet<TDataTuple>(
        this INaryMap<Schema<TDataTuple>> map)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        if (map is ISet<TDataTuple> set) return set;
        throw new InvalidOperationException("Unexpected map type.");
    }
    
    public static IReadOnlySet<TDataTuple> AsReadOnlySet<TDataTuple>(
        this IReadOnlyNaryMap<Schema<TDataTuple>> map)
        where TDataTuple : struct, ITuple, IStructuralEquatable
    {
        if (map is IReadOnlySet<TDataTuple> set) return set;
        throw new InvalidOperationException("Unexpected map type.");
    }
}