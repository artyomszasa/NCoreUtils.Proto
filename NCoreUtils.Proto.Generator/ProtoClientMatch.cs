using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoClientMatch(
    SemanticModel semanticModel,
    ClassDeclarationSyntax cds,
    INamedTypeSymbol clientType,
    ITypeSymbol infoType,
    ITypeSymbol? jsonSerializerContext,
    string? path,
    bool noHttpClientFactory,
    IReadOnlyList<ProtoClientConstructorParameter> additionalConstructorParameters,
    IReadOnlyDictionary<string, string> methodPaths)
{
    public SemanticModel SemanticModel { get; } = semanticModel;

    public ClassDeclarationSyntax Cds { get; } = cds;

    public INamedTypeSymbol ClientType { get; } = clientType;

    public ITypeSymbol InfoType { get; } = infoType;

    public ITypeSymbol? JsonSerializerContext { get; } = jsonSerializerContext;

    public string? Path { get; } = path;

    public bool NoHttpClientFactory { get; } = noHttpClientFactory;

    public IReadOnlyList<ProtoClientConstructorParameter> AdditionalConstructorParameters { get; } = additionalConstructorParameters;

    public IReadOnlyDictionary<string, string> MethodPaths { get; } = methodPaths;
}