using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoClientMatchBuilder(SemanticModel semanticModel, ClassDeclarationSyntax cds)
{
    private static IReadOnlyDictionary<string, string> NoMethodPaths { get; } = new Dictionary<string, string>();

    public SemanticModel SemanticModel { get; } = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

    public ClassDeclarationSyntax Cds { get; } = cds ?? throw new ArgumentNullException(nameof(cds));

    public ITypeSymbol? InfoType { get; set; }

    public ITypeSymbol? JsonSerializerContext { get; set; }

    public string? Path { get; set; }

    private Dictionary<string, string>? _methodPaths;

    public Dictionary<string, string> MethodPaths => _methodPaths ??= new();

    [MemberNotNullWhen(true, nameof(InfoType))]
    public bool IsValid => InfoType is not null;

    public ProtoClientMatch Build() => new(
        SemanticModel,
        Cds,
        InfoType ?? throw new InvalidOperationException("Info type must be defined."),
        JsonSerializerContext,
        Path,
        _methodPaths ?? NoMethodPaths
    );
}