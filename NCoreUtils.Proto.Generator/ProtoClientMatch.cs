using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoClientMatch(
    SemanticModel semanticModel,
    ClassDeclarationSyntax cds,
    ITypeSymbol infoType,
    ITypeSymbol? jsonSerializerContext,
    string? path,
    IReadOnlyDictionary<string, string> methodPaths)
{
    public SemanticModel SemanticModel { get; } = semanticModel;

    public ClassDeclarationSyntax Cds { get; } = cds;

    public ITypeSymbol InfoType { get; } = infoType;

    public ITypeSymbol? JsonSerializerContext { get; } = jsonSerializerContext;

    public string? Path { get; } = path;

    public IReadOnlyDictionary<string, string> MethodPaths { get; } = methodPaths;
}