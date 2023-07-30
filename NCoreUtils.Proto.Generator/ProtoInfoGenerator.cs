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
public class ProtoInfoGenerator : IIncrementalGenerator
{
    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Proto
{
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
public class ProtoInfoAttribute : System.Attribute
{
    public System.Type Target { get; }

    public InputType Input { get; set; }

    public OutputType Output { get; set; }

    public ErrorType Error { get; set; }

    public Naming Naming { get; set; }

    public Naming ParameterNaming { get; set; }

    public SingleJsonParameterWrapping SingleJsonParameterWrapping { get; set; }

    public bool KeepAsyncSuffix { get; set; }

    public string? Path { get; set; }

    public ProtoInfoAttribute(System.Type target)
        => Target = target ?? throw new System.ArgumentNullException(nameof(target));
}

[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public class ProtoMethodInfoAttribute : System.Attribute
{
    public string MethodName { get; }

    public InputType Input { get; set; }

    public OutputType Output { get; set; }

    public ErrorType Error { get; set; }

    public Naming Naming { get; set; }

    public Naming ParameterNaming { get; set; }

    public SingleJsonParameterWrapping SingleJsonParameterWrapping { get; set; }

    public bool KeepAsyncSuffix { get; set; }

    public string? Path { get; set; }

    public ProtoMethodInfoAttribute(string methodName)
        => MethodName = methodName ?? throw new System.ArgumentNullException(nameof(methodName));
}
}";

    private static UTF8Encoding Utf8 { get; } = new(false);

    private static T GetConstantAsEnum<T>(SemanticModel semanticModel, ExpressionSyntax expression)
        where T : struct
    {
        var svalue = semanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } when value is not null => value.ToString(),
            _ => throw new InvalidOperationException($"Unable to get constant value from {expression}")
        };
        return Enum.TryParse<T>(
            svalue,
            out var evalue
        )   ? evalue
            : throw new InvalidOperationException($"Unable to convert {svalue} to {typeof(T)}.");
    }

    private static bool GetConstantAsBoolean(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        return semanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } when value is bool bvalue => bvalue,
            _ => throw new InvalidOperationException($"Unable to get constant boolean value from {expression}")
        };
    }

    private static string? GetConstantAsMaybeString(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        return semanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } => value?.ToString(),
            _ => throw new InvalidOperationException($"Unable to get string? value from {expression}")
        };
    }

    private static string GetConstantAsString(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        return semanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } when value is not null => value.ToString(),
            _ => throw new InvalidOperationException($"Unable to get string value from {expression}")
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

    private static ProtoInfoMatch? GetTargetOrNull(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetNode is ClassDeclarationSyntax cds)
        {
            ProtoInfoMatchBuilder? target = default;
            var attributes = cds.AttributeLists.SelectMany(list => list.Attributes);
            foreach (var attribute in attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }
                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();
                // Logs.Add($"[{cds.Identifier}] attribute symbol: {fullName}");

                if (fullName == "ProtoInfoAttribute" || fullName == "NCoreUtils.Proto.ProtoInfoAttribute")
                {
                    if (target is not null)
                    {
                        throw new InvalidOperationException("Multiple ProtoInfoAttribute are not allowed.");
                    }
                    target = new ProtoInfoMatchBuilder(context.SemanticModel, cds);
                    var args = (IReadOnlyList<AttributeArgumentSyntax>?)attribute.ArgumentList?.Arguments ?? Array.Empty<AttributeArgumentSyntax>();
                    var i = 0;
                    foreach (var arg in args)
                    {
                        if (arg.NameEquals is null)
                        {
                            if (i != 0)
                            {
                                throw new InvalidOperationException("ProtoInfoAttribute must contain single not named argument.");
                            }
                            ++i;
                            target.TargetType = context.SemanticModel.GetTypeInfo(arg.ChildNodes().Single().ChildNodes().Single()).ConvertedType;
                        }
                        else
                        {
                            switch (arg.NameEquals.Name.Identifier.ValueText)
                            {
                                case "Input":
                                    target.Input = GetConstantAsEnum<ProtoInputType>(context.SemanticModel, arg.Expression);
                                    break;
                                case "Output":
                                    target.Output = GetConstantAsEnum<ProtoOutputType>(context.SemanticModel, arg.Expression);
                                    break;
                                case "Error":
                                    target.Error = GetConstantAsEnum<ProtoErrorType>(context.SemanticModel, arg.Expression);
                                    break;
                                case "Naming":
                                    target.Naming = GetConstantAsEnum<ProtoNaming>(context.SemanticModel, arg.Expression);
                                    break;
                                case "ParameterNaming":
                                    target.ParameterNaming = GetConstantAsEnum<ProtoNaming>(context.SemanticModel, arg.Expression);
                                    break;
                                case "SingleJsonParameterWrapping":
                                    target.SingleJsonParameterWrapping = GetConstantAsEnum<ProtoSingleJsonParameterWrapping>(context.SemanticModel, arg.Expression);
                                    break;
                                case "KeepAsyncSuffix":
                                    target.KeepAsyncSuffix = GetConstantAsBoolean(context.SemanticModel, arg.Expression);
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
                foreach (var attribute in attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }
                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();
                    if (fullName == "ProtoMethodInfoAttribute" || fullName == "NCoreUtils.Proto.ProtoMethodInfoAttribute")
                    {
                        string? methodName = default;
                        MethodGenerationOptionsBuilder opts = new();
                        var args = (IReadOnlyList<AttributeArgumentSyntax>?)attribute.ArgumentList?.Arguments ?? Array.Empty<AttributeArgumentSyntax>();
                        var i = 0;
                        foreach (var arg in args)
                        {
                            if (arg.NameEquals is null)
                            {
                                if (i != 0)
                                {
                                    throw new InvalidOperationException("ProtoMethodInfoAttribute must contain single not named argument.");
                                }
                                ++i;
                                methodName = GetConstantAsString(context.SemanticModel, arg.Expression);
                            }
                            else
                            {
                                switch (arg.NameEquals.Name.Identifier.ValueText)
                                {
                                    case "Input":
                                        opts.Input = GetConstantAsEnum<ProtoInputType>(context.SemanticModel, arg.Expression);
                                        break;
                                    case "Output":
                                        opts.Output = GetConstantAsEnum<ProtoOutputType>(context.SemanticModel, arg.Expression);
                                        break;
                                    case "Error":
                                        opts.Error = GetConstantAsEnum<ProtoErrorType>(context.SemanticModel, arg.Expression);
                                        break;
                                    case "Naming":
                                        opts.Naming = GetConstantAsEnum<ProtoNaming>(context.SemanticModel, arg.Expression);
                                        break;
                                    case "ParameterNaming":
                                        opts.ParameterNaming = GetConstantAsEnum<ProtoNaming>(context.SemanticModel, arg.Expression);
                                        break;
                                    case "SingleJsonParameterWrapping":
                                        opts.SingleJsonParameterWrapping = GetConstantAsEnum<ProtoSingleJsonParameterWrapping>(context.SemanticModel, arg.Expression);
                                        break;
                                    case "KeepAsyncSuffix":
                                        opts.KeepAsyncSuffix = GetConstantAsBoolean(context.SemanticModel, arg.Expression);
                                        break;
                                    case "Path":
                                        opts.Path = GetConstantAsMaybeString(context.SemanticModel, arg.Expression);
                                        break;
                                }
                            }
                        }
                        if (methodName is null || !target.TargetType.GetMembers().OfType<IMethodSymbol>().TryGetFirst(m => m.Name == methodName, out var meth))
                        {
                            throw new InvalidOperationException($"Could not find method {methodName} for type {target.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                        }
                        target.MethodOptions.Add(methodName, opts.Build(meth));
                    }
                }
                return target.Build();
            }
        }
        return null;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("Attributes.g.cs", SourceText.From(attributeSource, Utf8)));
        IncrementalValuesProvider<ProtoInfoMatch> matches = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NCoreUtils.Proto.ProtoInfoAttribute",
            (node, _) => node is ClassDeclarationSyntax,
            GetTargetOrNull
        ).Where(match => match is not null)!;

        context.RegisterSourceOutput(matches, static (ctx, match) =>
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            var info = new ProtoInfoParser(match.SemanticModel).ParseInfo(ctx, match);
            var code = new ProtoInfoEmitter(info).EmitServiceInfo(GetSyntaxNamespace(match.Cds) ?? "NCoreUtils.Proto.Generated", match.Cds.Identifier.ValueText);
            ctx.AddSource($"{match.Cds.Identifier.ValueText}.g.cs", SourceText.From(code, Utf8));
        });
    }
}
