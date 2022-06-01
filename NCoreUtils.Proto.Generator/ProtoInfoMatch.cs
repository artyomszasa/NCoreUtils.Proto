using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoInfoMatch
{
    public SemanticModel SemanticModel { get; }

    public ClassDeclarationSyntax Cds { get; }

    public ITypeSymbol TargetType { get; }

    public ProtoInputType Input { get; }

    public ProtoOutputType Output { get; }

    public ProtoErrorType Error { get; }

    public ProtoNaming Naming { get; }

    public ProtoNaming? ParameterNaming { get; }

    public ProtoSingleJsonParameterWrapping? SingleJsonParameterWrapping { get; }

    public bool KeepAsyncSuffix { get; }

    public string? Path { get; }

    public IReadOnlyDictionary<string, MethodGenerationOptions> MethodOptions { get; }

    public ProtoInfoMatch(
        SemanticModel semanticModel,
        ClassDeclarationSyntax cds,
        ITypeSymbol targetType,
        ProtoInputType input,
        ProtoOutputType output,
        ProtoErrorType error,
        ProtoNaming naming,
        ProtoNaming? parameterNaming,
        ProtoSingleJsonParameterWrapping? singleJsonParameterWrapping,
        bool keepAsyncSuffix,
        string? path,
        IReadOnlyDictionary<string, MethodGenerationOptions> methodOptions)
    {
        SemanticModel = semanticModel;
        Cds = cds;
        TargetType = targetType;
        Input = input;
        Output = output;
        Error = error;
        Naming = naming;
        ParameterNaming = parameterNaming;
        SingleJsonParameterWrapping = singleJsonParameterWrapping;
        KeepAsyncSuffix = keepAsyncSuffix;
        Path = path;
        MethodOptions = methodOptions;
    }
}