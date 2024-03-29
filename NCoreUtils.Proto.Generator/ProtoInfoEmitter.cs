using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoInfoEmitter
{
    private static string NewLine { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "\r\n"
        : "\n";

    private ProtoServiceInfo Info { get; }

    public ProtoInfoEmitter(ProtoServiceInfo info)
        => Info = info ?? throw new ArgumentNullException(nameof(info));

    private string EmitMethodInfo(MethodDescriptor desc)
        => @$"public sealed class {desc.MethodId}Info
        : global::NCoreUtils.Proto.Internal.ProtoMethodInfo
        , {(desc.NoReturn ? $"global::NCoreUtils.Proto.Internal.IProtoMethodVoidReturn<{desc.ReturnType}>" : $"global::NCoreUtils.Proto.Internal.IProtoMethodReturn<{desc.ReturnType}, {desc.ReturnValueType}>")}
        {(desc.InputDtoTypeName is null ? string.Empty : $", global::NCoreUtils.Proto.Internal.IProtoMethodInputDto<{desc.InputDtoTypeName}>")}
    {{
        public const string MethodName = ""{desc.MethodName}"";

        public const string MethodId = ""{desc.MethodId}"";

        public const global::NCoreUtils.Proto.InputType Input = global::NCoreUtils.Proto.InputType.{desc.Input};

        public const global::NCoreUtils.Proto.OutputType Output = global::NCoreUtils.Proto.OutputType.{desc.Output};

        public const global::NCoreUtils.Proto.ErrorType Error = global::NCoreUtils.Proto.ErrorType.{desc.Error};

        {(desc.ParameterNaming.HasValue ? $"public const global::NCoreUtils.Proto.Naming ParameterNaming = global::NCoreUtils.Proto.Naming.{desc.ParameterNaming.Value};" : string.Empty)}

        public const global::NCoreUtils.Proto.SingleJsonParameterWrapping SingleJsonParameterWrapping = global::NCoreUtils.Proto.SingleJsonParameterWrapping.{desc.SingleJsonParameterWrapping};

        public const string Path = ""{desc.Path}"";

        public const bool NoReturn = {(desc.NoReturn ? "true" : "false")};
    }}";

    private string EmitInputDto(MethodDescriptor desc)
    {
        if (desc.SingleJsonParameterWrapping == ProtoSingleJsonParameterWrapping.DoNotWrap && desc.Parameters.Count == 1)
        {
            return string.Empty;
        }
        return @$"public class {desc.InputDtoTypeName}
{{
    {string.Join(NewLine + "    ", desc.Parameters.Select(EmitProperty))}

    public {desc.InputDtoTypeName}({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))})
    {{
        {string.Join(NewLine + "        ", desc.Parameters.Select(e => $"this.{e.Name} = {e.Name};"))}
    }}
}};";

        static string EmitProperty(ParameterDescriptor e)
        {
            if (e.ConverterType is null)
            {
                return $"public {e.TypeName} {e.Name} {{ get; }}";
            }
            return @$"[System.Text.Json.Serialization.JsonConverterAttribute(typeof({e.ConverterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))]
    public {e.TypeName} {e.Name} {{ get; }}";
        }
    }

    private string EmitRootSerializationClass(string name)
        => @$"public class JsonRoot{name}
{{
    {string.Join(NewLine + "    ", Info.Methods.Where(m => m.Input == ProtoInputType.Json).Select(m => $"public {m.InputDtoTypeName} {m.MethodId}Args {{ get; set; }} = default!;"))}

    {string.Join(NewLine + "    ", Info.Methods.Where(m => !m.NoReturn && m.Output == ProtoOutputType.Json).Select(m => $"public {m.ReturnValueType} {m.MethodId}Result {{ get; set; }} = default!;"))}
}}";

    public string EmitServiceInfo(string @namespace, string name)
    {
        return $@"#nullable enable
namespace {@namespace}
{{
public partial class {name} : global::NCoreUtils.Proto.Internal.ProtoServiceInfo<{Info.TargetFullName}>
{{
    {string.Join(NewLine + "    ", Info.Methods.Select(EmitMethodInfo))}

    public const string Path = ""{Info.Path}"";
}}
{string.Join(NewLine, Info.Methods.Select(EmitInputDto))}
{EmitRootSerializationClass(name)}
}}";
    }
}