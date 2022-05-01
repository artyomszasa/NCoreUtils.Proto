using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoClientMatch
{
    public SemanticModel SemanticModel { get; }

    public ClassDeclarationSyntax Cds { get; }

    public ITypeSymbol InfoType { get; }

    public ITypeSymbol? JsonSerializerContext { get; }

    public string? Path { get; }

    public IReadOnlyDictionary<string, string> MethodPaths { get; }

    public ProtoClientMatch(
        SemanticModel semanticModel,
        ClassDeclarationSyntax cds,
        ITypeSymbol infoType,
        ITypeSymbol? jsonSerializerContext,
        string? path,
        IReadOnlyDictionary<string, string> methodPaths)
    {
        SemanticModel = semanticModel;
        Cds = cds;
        InfoType = infoType;
        JsonSerializerContext = jsonSerializerContext;
        Path = path;
        MethodPaths = methodPaths;
    }
}