using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Proto;

[Generator]
public class ProtoClientGenerator : IIncrementalGenerator
{
    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Proto
{
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
internal class ProtoClientAttribute : System.Attribute
{
    public System.Type Info { get; }

    public System.Type JsonSerializerContext { get; }

    public string? Path { get; set; }

    public ProtoClientAttribute(System.Type info, System.Type jsonSerializerContext)
    {
        Info = info ?? throw new System.ArgumentNullException(nameof(info));
        JsonSerializerContext = jsonSerializerContext ?? throw new System.ArgumentNullException(nameof(jsonSerializerContext));
    }
}

[System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
internal class HandlesResponseDisposalAttribute : System.Attribute
{
    public HandlesResponseDisposalAttribute() { /* noop */ }
}

}";

    private static UTF8Encoding Utf8 { get; } = new(false);

    private static string? GetConstantAsMaybeString(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        return semanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } => value?.ToString(),
            _ => throw new InvalidOperationException($"Unable to get string? value from {expression}")
        };
    }

    private static string? GetSyntaxNamespace(SyntaxNode node)
    {
        if (node is NamespaceDeclarationSyntax ns)
        {
            return ns.Name.ToString();
        }
        if (node is FileScopedNamespaceDeclarationSyntax fns)
        {
            return fns.Name.ToString();
        }
        if (node.Parent is null)
        {
            return default;
        }
        return GetSyntaxNamespace(node.Parent);
    }

    private static ProtoClientMatch? GetTargetOrNull(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.TargetNode is ClassDeclarationSyntax cds && ctx.TargetSymbol is INamedTypeSymbol namedTargetSymbol)
        {
            ProtoClientMatchBuilder? target = default;
            var attributes = cds.AttributeLists.SelectMany(list => list.Attributes);
            foreach (var attribute in attributes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ctx.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }
                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();
                if (fullName == "ProtoClientAttribute" || fullName == "NCoreUtils.Proto.ProtoClientAttribute")
                {
                    if (target is not null)
                    {
                        throw new InvalidOperationException("Multiple ProtoClientAttribute are not allowed.");
                    }
                    target = new ProtoClientMatchBuilder(ctx.SemanticModel, cds, namedTargetSymbol);
                    var args = (IReadOnlyList<AttributeArgumentSyntax>?)attribute.ArgumentList?.Arguments ?? Array.Empty<AttributeArgumentSyntax>();
                    var i = 0;
                    foreach (var arg in args)
                    {
                        if (arg.NameEquals is null)
                        {
                            if (i > 1)
                            {
                                throw new InvalidOperationException("ProtoClientAttribute must contain exactly two not named argument.");
                            }
                            if (i == 0)
                            {
                                target.InfoType = ctx.SemanticModel.GetTypeInfo(arg.ChildNodes().Single().ChildNodes().Single()).ConvertedType;
                            }
                            else
                            {
                                target.JsonSerializerContext = ctx.SemanticModel.GetTypeInfo(arg.ChildNodes().Single().ChildNodes().Single()).ConvertedType;
                            }
                            ++i;
                        }
                        else
                        {
                            switch (arg.NameEquals.Name.Identifier.ValueText)
                            {
                                case "Path":
                                    target.Path = GetConstantAsMaybeString(ctx.SemanticModel, arg.Expression);
                                    break;
                            }
                        }
                    }
                }
                if (target is not null && target.IsValid)
                {
                    return target.Build();
                }
            }
        }
        return null;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("Attributes.g.cs", SourceText.From(attributeSource, Utf8)));
        IncrementalValuesProvider<ProtoClientMatch> matches = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NCoreUtils.Proto.ProtoClientAttribute",
            (node, _) => node is ClassDeclarationSyntax,
            GetTargetOrNull
        ).Where(match => match is not null)!;

        context.RegisterSourceOutput(matches, static (ctx, match) =>
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            var client = new ProtoClientParser(match.SemanticModel).Parse(match);
            var code = new ProtoClientEmitter(client).EmitClient(GetSyntaxNamespace(match.Cds) ?? "NCoreUtils.Proto.Generated", match.Cds.Identifier.ValueText);
            ctx.AddSource($"{match.Cds.Identifier.ValueText}.g.cs", SourceText.From(code, Utf8));
        });
    }
}