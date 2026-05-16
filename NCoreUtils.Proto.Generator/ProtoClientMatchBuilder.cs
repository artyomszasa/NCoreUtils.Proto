using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoClientMatchBuilder(SemanticModel semanticModel, ClassDeclarationSyntax cds, INamedTypeSymbol clientType)
{
    private static IReadOnlyDictionary<string, string> NoMethodPaths { get; } = new Dictionary<string, string>();

    private List<ProtoClientConstructorParameter>? _additionalConstructorParameters;

    public SemanticModel SemanticModel { get; } = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

    public INamedTypeSymbol ClientType { get; } = clientType ?? throw new ArgumentNullException(nameof(clientType));

    public ClassDeclarationSyntax Cds { get; } = cds ?? throw new ArgumentNullException(nameof(cds));

    public ITypeSymbol? InfoType { get; set; }

    public ITypeSymbol? JsonSerializerContext { get; set; }

    public string? Path { get; set; }

    public bool NoHttpClientFactory { get; set; }

    private Dictionary<string, string>? _methodPaths;

    public Dictionary<string, string> MethodPaths => _methodPaths ??= new();

    [MemberNotNullWhen(true, nameof(InfoType))]
    public bool IsValid => InfoType is not null;

    public void AddAdditionalConstructorParameter(ProtoClientConstructorParameter parameter)
        => (_additionalConstructorParameters ??= []).Add(parameter);

    public void AddAdditionalConstructorParameter(ITypeSymbol type, string name)
        => AddAdditionalConstructorParameter(new (type, name));

    public ProtoClientMatch Build() => new(
        SemanticModel,
        Cds,
        ClientType,
        InfoType ?? throw new InvalidOperationException("Info type must be defined."),
        JsonSerializerContext,
        Path,
        NoHttpClientFactory,
        _additionalConstructorParameters is List<ProtoClientConstructorParameter> parameters
            ? parameters.ToArray()
            : [],
        _methodPaths ?? NoMethodPaths
    );
}