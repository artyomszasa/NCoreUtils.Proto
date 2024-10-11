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
public class ProtoImplGenerator : IIncrementalGenerator
{
    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Proto
{
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
internal class ProtoServiceAttribute : System.Attribute
{
    public System.Type Info { get; }

    public System.Type JsonSerializerContext { get; }

    public string? Path { get; set; }

    public System.Type? ImplementationFactory { get; set; }

    public ProtoServiceAttribute(System.Type info, System.Type jsonSerializerContext)
    {
        Info = info ?? throw new System.ArgumentNullException(nameof(info));
        JsonSerializerContext = jsonSerializerContext ?? throw new System.ArgumentNullException(nameof(jsonSerializerContext));
    }
}
}";

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

    private static UTF8Encoding Utf8 { get; } = new(false);

    private static ProtoImplMatch? GetTargetOrNull(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetNode is ClassDeclarationSyntax cds && context.TargetSymbol is INamedTypeSymbol namedTargetSymbol)
        {
            ProtoImplMatchBuilder? target = default;
            var attributes = cds.AttributeLists.SelectMany(list => list.Attributes);
            foreach (var attribute in attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }
                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();
                if (fullName == "ProtoServiceAttribute" || fullName == "NCoreUtils.Proto.ProtoServiceAttribute")
                {
                    if (target is not null)
                    {
                        throw new InvalidOperationException("Multiple ProtoServiceAttribute are not allowed.");
                    }
                    target = new ProtoImplMatchBuilder(context.SemanticModel, cds, namedTargetSymbol);
                    var args = (IReadOnlyList<AttributeArgumentSyntax>?)attribute.ArgumentList?.Arguments ?? Array.Empty<AttributeArgumentSyntax>();
                    var i = 0;
                    foreach (var arg in args)
                    {
                        if (arg.NameEquals is null)
                        {
                            if (i > 1)
                            {
                                throw new InvalidOperationException("ProtoServiceAttribute must contain exactly two not named argument.");
                            }
                            if (i == 0)
                            {
                                target.InfoType = context.SemanticModel.GetTypeInfo(arg.ChildNodes().Single().ChildNodes().Single()).ConvertedType;
                            }
                            else
                            {
                                target.JsonSerializerContext = context.SemanticModel.GetTypeInfo(arg.ChildNodes().Single().ChildNodes().Single()).ConvertedType;
                            }
                            ++i;
                        }
                        else
                        {
                            switch (arg.NameEquals.Name.Identifier.ValueText)
                            {
                                case "ImplementationFactory":
                                    target.ImplementationFactory = context.SemanticModel.GetTypeInfo(arg.Expression.ChildNodes().Single()).ConvertedType;
                                    break;
                                case "Path":
                                    target.Path = GetConstantAsMaybeString(context.SemanticModel, arg.Expression);
                                    break;
                            }
                        }
                    }
                }
            }
            if (target is not null && target.IsValid)
            {
                return target.Build();
            }
        }
        return null;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("Attributes.g.cs", SourceText.From(attributeSource, Utf8)));
        IncrementalValuesProvider<ProtoImplMatch> matches = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NCoreUtils.Proto.ProtoServiceAttribute",
            (node, _) => node is ClassDeclarationSyntax,
            GetTargetOrNull
        ).Where(match => match is not null)!;

        context.RegisterSourceOutput(matches, static (ctx, match) =>
        {
            var service = new ProtoImplParser(match.SemanticModel).Parse(match);
            // NOTE: try to get any partial implementations
            var @namespace = GetSyntaxNamespace(match.Cds) ?? "NCoreUtils.Proto.Generated";
            var rootName = match.Cds.Identifier.ValueText;
            var name = "Proto" + rootName  + "Implementation";
            var ty = match.SemanticModel.Compilation.GetTypeByMetadataName(@namespace + "." + name);
            var code = new ProtoImplEmitter(service, new ProtoImplEmitterContext(match.SemanticModel))
                .EmitImpl(
                    @namespace,
                    rootName,
                    name,
                    ty
                );
            ctx.AddSource($"{rootName}.g.cs", SourceText.From(code, Utf8));
        });
    }
}