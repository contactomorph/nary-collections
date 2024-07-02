using System.Reflection;

namespace NaryCollections.Components;

internal static class CommonCompilation
{
    public static readonly MethodAttributes ProjectorMethodAttributes =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final;

    public static Type GetItemType(DataTypeProjection dataTypeProjection)
    {
        var dataMappingOutput = dataTypeProjection.DataProjectionMapping.OutputType;
        return dataMappingOutput.Count == 1 ? dataMappingOutput[0].FieldType : dataMappingOutput;
    }
}