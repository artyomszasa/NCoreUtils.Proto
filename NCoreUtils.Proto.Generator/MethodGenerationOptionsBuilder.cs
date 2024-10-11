using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public class MethodGenerationOptionsBuilder
{
    public ProtoInputType? Input { get; set; }

    public ProtoOutputType? Output { get; set; }

    public ProtoErrorType? Error { get; set; }

    public ProtoNaming? Naming { get; set; }

    public ProtoNaming? ParameterNaming { get; set; }

    public ProtoSingleJsonParameterWrapping? SingleJsonParameterWrapping { get; set; }

    public ProtoHttpMethod HttpMethod { get; set; }

    public bool? KeepAsyncSuffix { get; set; }

    public string? Path { get; set; }

    public MethodGenerationOptions Build(IMethodSymbol method) => new(
        method,
        Input,
        Output,
        Error,
        Naming,
        ParameterNaming,
        SingleJsonParameterWrapping,
        HttpMethod,
        KeepAsyncSuffix,
        Path
    );
}