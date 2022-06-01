using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public class MethodGenerationOptions
{
    public IMethodSymbol Method { get; }

    public ProtoInputType? Input { get; }

    public ProtoOutputType? Output { get; }

    public ProtoErrorType? Error { get; }

    public ProtoNaming? Naming { get; }

    public ProtoNaming? ParameterNaming { get; }

    public ProtoSingleJsonParameterWrapping? SingleJsonParameterWrapping { get; }

    public bool? KeepAsyncSuffix { get; }

    public string? Path { get; }

    public MethodGenerationOptions(
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
        Method = method;
        Input = input;
        Output = output;
        Error = error;
        Naming = naming;
        ParameterNaming = parameterNaming;
        SingleJsonParameterWrapping = singleJsonParameterWrapping;
        KeepAsyncSuffix = keepAsyncSuffix;
        Path = path;
    }
}