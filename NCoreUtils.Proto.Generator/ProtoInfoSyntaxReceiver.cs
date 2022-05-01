using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

public class ProtoInfoSyntaxReceiver : ISyntaxContextReceiver
{
    private static T GetConstantAsEnum<T>(GeneratorSyntaxContext context, ExpressionSyntax expression)
        where T : struct
    {
        var svalue = context.SemanticModel.GetConstantValue(expression) switch
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

    private static bool GetConstantAsBoolean(GeneratorSyntaxContext context, ExpressionSyntax expression)
    {
        return context.SemanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } when value is bool bvalue => bvalue,
            _ => throw new InvalidOperationException($"Unable to get constant boolean value from {expression}")
        };
    }

    private static string? GetConstantAsMaybeString(GeneratorSyntaxContext context, ExpressionSyntax expression)
    {
        return context.SemanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } => value?.ToString(),
            _ => throw new InvalidOperationException($"Unable to get string? value from {expression}")
        };
    }

    private static string GetConstantAsString(GeneratorSyntaxContext context, ExpressionSyntax expression)
    {
        return context.SemanticModel.GetConstantValue(expression) switch
        {
            { HasValue: true, Value: var value } when value is not null => value.ToString(),
            _ => throw new InvalidOperationException($"Unable to get string value from {expression}")
        };
    }

    internal List<ProtoInfoMatch>? Matches { get; set; }

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax cds)
        {
            ProtoInfoMatchBuilder? target = default;

            // Logs.Add($"CDS: {cds.Identifier}");
            var attributes = cds.AttributeLists.SelectMany(list => list.Attributes);
            foreach (var attribute in attributes)
            {
                // Logs.Add($"[{cds.Identifier}] attribute: {attribute.FullSpan.ToString()}");
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    // Logs.Add($"[{cds.Identifier}] attribute symbol is null");
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
                                    target.Input = GetConstantAsEnum<ProtoInputType>(context, arg.Expression);
                                    break;
                                case "Output":
                                    target.Output = GetConstantAsEnum<ProtoOutputType>(context, arg.Expression);
                                    break;
                                case "Error":
                                    target.Error = GetConstantAsEnum<ProtoErrorType>(context, arg.Expression);
                                    break;
                                case "Naming":
                                    target.Naming = GetConstantAsEnum<ProtoNaming>(context, arg.Expression);
                                    break;
                                case "ParameterNaming":
                                    target.ParameterNaming = GetConstantAsEnum<ProtoNaming>(context, arg.Expression);
                                    break;
                                case "KeepAsyncSuffix":
                                    target.KeepAsyncSuffix = GetConstantAsBoolean(context, arg.Expression);
                                    break;
                                case "Path":
                                    target.Path = GetConstantAsMaybeString(context, arg.Expression);
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
                    // Logs.Add($"[{cds.Identifier}] attribute: {attribute.FullSpan.ToString()}");
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        // Logs.Add($"[{cds.Identifier}] attribute symbol is null");
                        continue;
                    }
                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();
                    // Logs.Add($"[{cds.Identifier}] attribute symbol: {fullName}");

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
                                methodName = GetConstantAsString(context, arg.Expression);
                            }
                            else
                            {
                                switch (arg.NameEquals.Name.Identifier.ValueText)
                                {
                                    case "Input":
                                        opts.Input = GetConstantAsEnum<ProtoInputType>(context, arg.Expression);
                                        break;
                                    case "Output":
                                        opts.Output = GetConstantAsEnum<ProtoOutputType>(context, arg.Expression);
                                        break;
                                    case "Error":
                                        opts.Error = GetConstantAsEnum<ProtoErrorType>(context, arg.Expression);
                                        break;
                                    case "Naming":
                                        opts.Naming = GetConstantAsEnum<ProtoNaming>(context, arg.Expression);
                                        break;
                                    case "ParameterNaming":
                                        opts.ParameterNaming = GetConstantAsEnum<ProtoNaming>(context, arg.Expression);
                                        break;
                                    case "KeepAsyncSuffix":
                                        opts.KeepAsyncSuffix = GetConstantAsBoolean(context, arg.Expression);
                                        break;
                                    case "Path":
                                        opts.Path = GetConstantAsMaybeString(context, arg.Expression);
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
                Matches ??= new();
                Matches.Add(target.Build());
            }
        }
    }
}