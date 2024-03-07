using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoInfoMatchBuilder(SemanticModel semanticModel, ClassDeclarationSyntax cds)
{
    public static IReadOnlyDictionary<string, MethodGenerationOptions> NoMethodOptions { get; }
        = new Dictionary<string, MethodGenerationOptions>();

    public SemanticModel SemanticModel { get; } = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

    public ClassDeclarationSyntax Cds { get; } = cds ?? throw new ArgumentNullException(nameof(cds));

    public ITypeSymbol? TargetType { get; set; }

    public ProtoInputType Input { get; set; }

    public ProtoOutputType Output { get; set; }

    public ProtoErrorType Error { get; set; }

    public ProtoNaming Naming { get; set; }

    public ProtoNaming? ParameterNaming { get; set; }

    public ProtoSingleJsonParameterWrapping? SingleJsonParameterWrapping { get; set; }

    public bool KeepAsyncSuffix { get; set; }

    public string? Path { get; set; }

    private Dictionary<string, MethodGenerationOptions>? _methodOptions;

    public Dictionary<string, MethodGenerationOptions> MethodOptions
        => _methodOptions ??= [];

    [MemberNotNullWhen(true, nameof(TargetType))]
    public bool IsValid => TargetType is not null;

    public ProtoInfoMatch Build() => IsValid
        ? new ProtoInfoMatch(
            SemanticModel,
            Cds,
            TargetType,
            Input,
            Output,
            Error,
            Naming,
            ParameterNaming,
            SingleJsonParameterWrapping,
            KeepAsyncSuffix,
            Path,
            _methodOptions ?? NoMethodOptions
        )
        : throw new InvalidOperationException("No target type defined.");
}