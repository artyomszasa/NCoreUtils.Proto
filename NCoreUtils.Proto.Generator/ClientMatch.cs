using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ClientMatch
{
    public SemanticModel SemanticModel { get; }

    public ClassDeclarationSyntax Cds { get; }

    public ITypeSymbol TargetType { get; }

    public ProtoInputType Input { get; }

    public ProtoOutputType Output { get; }

    public ProtoErrorType Error { get; }

    public ProtoNaming Naming { get; }

    public bool KeepAsyncSuffix { get; }

    public string? Path { get; }

    public string? HttpClientConfiguration { get; }

    public IReadOnlyDictionary<string, MethodGenerationOptions> MethodOptions { get; }

    public ClientMatch(
        SemanticModel semanticModel,
        ClassDeclarationSyntax cds,
        ITypeSymbol targetType,
        ProtoInputType input,
        ProtoOutputType output,
        ProtoErrorType error,
        ProtoNaming naming,
        bool keepAsyncSuffix,
        string? path,
        string? httpClientConfiguration,
        IReadOnlyDictionary<string, MethodGenerationOptions> methodOptions)
    {
        SemanticModel = semanticModel;
        Cds = cds;
        TargetType = targetType;
        Input = input;
        Output = output;
        Error = error;
        Naming = naming;
        KeepAsyncSuffix = keepAsyncSuffix;
        Path = path;
        HttpClientConfiguration = httpClientConfiguration;
        MethodOptions = methodOptions;
    }
}