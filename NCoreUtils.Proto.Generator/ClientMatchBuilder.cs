using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ClientMatchBuilder
{
    public static IReadOnlyDictionary<string, MethodGenerationOptions> NoMethodOptions { get; }
        = new Dictionary<string, MethodGenerationOptions>();

    public SemanticModel SemanticModel { get; }

    public ClassDeclarationSyntax Cds { get; }

    public ITypeSymbol? TargetType { get; set; }

    public ProtoInputType Input { get; set; }

    public ProtoOutputType Output { get; set; }

    public ProtoErrorType Error { get; set; }

    public ProtoNaming Naming { get; set; }

    public bool KeepAsyncSuffix { get; set; }

    public string? Path { get; set; }

    public string? HttpClientConfiguration { get; set; }

    private Dictionary<string, MethodGenerationOptions>? _methodOptions;

    public Dictionary<string, MethodGenerationOptions> MethodOptions
        => _methodOptions ??= new();

    [MemberNotNullWhen(true, nameof(TargetType))]
    public bool IsValid => TargetType is not null;

    public ClientMatchBuilder(SemanticModel semanticModel, ClassDeclarationSyntax cds)
    {
        SemanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
        Cds = cds ?? throw new ArgumentNullException(nameof(cds));
    }

    public ClientMatch Build() => IsValid
        ? new ClientMatch(SemanticModel, Cds, TargetType, Input, Output, Error, Naming, KeepAsyncSuffix, Path, HttpClientConfiguration, _methodOptions ?? NoMethodOptions)
        : throw new InvalidOperationException("No target type defined.");
}