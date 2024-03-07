using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoInfoMatch(
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
    public SemanticModel SemanticModel { get; } = semanticModel;

    public ClassDeclarationSyntax Cds { get; } = cds;

    public ITypeSymbol TargetType { get; } = targetType;

    public ProtoInputType Input { get; } = input;

    public ProtoOutputType Output { get; } = output;

    public ProtoErrorType Error { get; } = error;

    public ProtoNaming Naming { get; } = naming;

    public ProtoNaming? ParameterNaming { get; } = parameterNaming;

    public ProtoSingleJsonParameterWrapping? SingleJsonParameterWrapping { get; } = singleJsonParameterWrapping;

    public bool KeepAsyncSuffix { get; } = keepAsyncSuffix;

    public string? Path { get; } = path;

    public IReadOnlyDictionary<string, MethodGenerationOptions> MethodOptions { get; } = methodOptions;
}