using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public class MethodGenerationOptions(
    IMethodSymbol method,
    ProtoInputType? input,
    ProtoOutputType? output,
    ProtoErrorType? error,
    ProtoNaming? naming,
    ProtoNaming? parameterNaming,
    ProtoSingleJsonParameterWrapping? singleJsonParameterWrapping,
    bool? keepAsyncSuffix,
    string? path)
{
    public IMethodSymbol Method { get; } = method;

    public ProtoInputType? Input { get; } = input;

    public ProtoOutputType? Output { get; } = output;

    public ProtoErrorType? Error { get; } = error;

    public ProtoNaming? Naming { get; } = naming;

    public ProtoNaming? ParameterNaming { get; } = parameterNaming;

    public ProtoSingleJsonParameterWrapping? SingleJsonParameterWrapping { get; } = singleJsonParameterWrapping;

    public bool? KeepAsyncSuffix { get; } = keepAsyncSuffix;

    public string? Path { get; } = path;
}