using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoImplMatch
{
    public SemanticModel SemanticModel { get; }

    public ClassDeclarationSyntax Cds { get; }

    public ITypeSymbol InfoType { get; }

    public ITypeSymbol? JsonSerializerContext { get; }

    public string? Path { get; }

    public IReadOnlyDictionary<string, string> MethodPaths { get; }

    public ITypeSymbol? ImplementationFactory { get; }

    public ProtoImplMatch(
        SemanticModel semanticModel,
        ClassDeclarationSyntax cds,
        ITypeSymbol infoType,
        ITypeSymbol? jsonSerializerContext,
        string? path,
        IReadOnlyDictionary<string, string> methodPaths,
        ITypeSymbol? implementationFactory)
    {
        SemanticModel = semanticModel;
        Cds = cds;
        InfoType = infoType;
        JsonSerializerContext = jsonSerializerContext;
        Path = path;
        MethodPaths = methodPaths;
        ImplementationFactory = implementationFactory;
    }
}