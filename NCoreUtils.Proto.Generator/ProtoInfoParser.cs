using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoInfoParser : ProtoParser
{
    private static DiagnosticDescriptor PropertyNotSupportedDescriptor { get; } = new DiagnosticDescriptor(
        id: "PROTO0001",
        title: "Unsupported member",
        messageFormat: "Property getters and setters are not supported (property = {0}).",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static string ApplyInterfaceNaming(ITypeSymbol targetType, INamingConvention convention)
    {
        var name = targetType.Name;
        var nameSpan = name.Length > 0 && name[0] == 'I' ? name.AsSpan(1) : name.AsSpan();
        var buffer = ArrayPool<char>.Shared.Rent(convention.GetMaxCharCount(nameSpan.Length));
        try
        {
            var len = convention.Apply(nameSpan, buffer);
            return new string(buffer, 0, len);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, false);
        }
    }

    private static string ApplyMethodNaming(IMethodSymbol method, bool keepAsyncSuffix, INamingConvention convention)
    {
        var name = method.Name;
        var nameSpan = !keepAsyncSuffix && name.EndsWith("Async") ? name.AsSpan(0, name.Length - 5) : name.AsSpan();
        var buffer = ArrayPool<char>.Shared.Rent(convention.GetMaxCharCount(nameSpan.Length));
        try
        {
            var len = convention.Apply(nameSpan, buffer);
            return new string(buffer, 0, len);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, false);
        }
    }

    public ProtoInfoParser(SemanticModel semanticModel)
        : base(semanticModel)
    { }

    public ProtoServiceInfo ParseInfo(SourceProductionContext context, ProtoInfoMatch match)
    {
        var tVoid = Compilation.GetSpecialType(SpecialType.System_Void);
        var rootPath = (match.Path ?? (match.Naming switch
        {
            ProtoNaming.CamelCase => ApplyInterfaceNaming(match.TargetType, NamingConvention.CamelCase),
            ProtoNaming.KebabCase => ApplyInterfaceNaming(match.TargetType, NamingConvention.KebabCase),
            ProtoNaming.PascalCase => ApplyInterfaceNaming(match.TargetType, NamingConvention.PascalCase),
            _ => ApplyInterfaceNaming(match.TargetType, NamingConvention.SnakeCase),
        })).Trim('/');
        var serviceId = match.TargetType.Name;

        var methods = match.TargetType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m =>
            {
                // NOTE: exclude property getters/setters
                if (m.MethodKind == MethodKind.PropertyGet || m.MethodKind == MethodKind.PropertySet)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: PropertyNotSupportedDescriptor,
                        location: m.Locations.FirstOrDefault(),
                        messageArgs: new object[] { m.AssociatedSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty }
                    ));
                    return false;
                }
                return true;
            })
            .Select(m =>
            {
                var (returnValueType, asyncReturnType) = m.ReturnType switch
                {
                    INamedTypeSymbol nts when Eqx(nts.ConstructedFrom, TypeTaskOf) => (nts.TypeArguments[0], AsyncReturnType.Task),
                    INamedTypeSymbol nts when Eqx(nts.ConstructedFrom, TypeValueTaskOf) => (nts.TypeArguments[0], AsyncReturnType.ValueTask),
                    INamedTypeSymbol nts when Eqx(nts.ConstructedFrom, TypeAsyncEnumerableOf) => (nts.TypeArguments[0], AsyncReturnType.AsyncEnumerable),
                    var ts when Eqx(ts, TypeTask) => (tVoid, AsyncReturnType.Task),
                    var ts when Eqx(ts, TypeValueTask) => (tVoid, AsyncReturnType.ValueTask),
                    var ts => throw new InvalidOperationException($"Method {m} has unsupported return type.")
                };
                var opts = match.MethodOptions.TryGetValue(m.Name, out var ox) ? ox : default;
                var parameterNaming = opts?.ParameterNaming ?? match.ParameterNaming;
                var (usesCancellation, parameters) = GetParameters(m, parameterNaming);
                var noReturn = Eqx(returnValueType, tVoid);
                var keepAsyncSuffix = opts?.KeepAsyncSuffix ?? match.KeepAsyncSuffix;
                var input = (opts?.Input ?? match.Input) switch
                {
                    ProtoInputType.Default when parameters.Count == 0 => ProtoInputType.Query,
                    ProtoInputType.Default => ProtoInputType.Json,
                    var i => i
                };
                var output = (opts?.Output ?? match.Output) switch
                {
                    ProtoOutputType.Default when !noReturn => ProtoOutputType.Json,
                    var o => o
                };
                var sjaw = opts?.SingleJsonParameterWrapping ?? match.SingleJsonParameterWrapping ?? ProtoSingleJsonParameterWrapping.DoNotWrap;

                // var path = '/' + rootPath.Trim('/') + '/' + (opts?.Path ?? ((opts?.Naming ?? match.Naming) switch
                // {
                //     ProtoNaming.CamelCase => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.CamelCase),
                //     ProtoNaming.KebabCase => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.KebabCase),
                //     ProtoNaming.PascalCase => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.PascalCase),
                //     _ => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.SnakeCase)
                // })).Trim('/');
                var path = (opts?.Path ?? ((opts?.Naming ?? match.Naming) switch
                {
                    ProtoNaming.CamelCase => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.CamelCase),
                    ProtoNaming.KebabCase => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.KebabCase),
                    ProtoNaming.PascalCase => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.PascalCase),
                    _ => ApplyMethodNaming(m, keepAsyncSuffix, NamingConvention.SnakeCase)
                })).Trim('/');
                var methodId = m.Name.EndsWith("Async") ? m.Name[..^5] : m.Name;

                return new MethodDescriptor(
                    returnType: m.ReturnType.ToFullMaybeNullableName(),
                    returnValueType: TypeName.Create(returnValueType),
                    noReturn: noReturn,
                    asyncReturnType: asyncReturnType,
                    methodName: m.Name,
                    methodId: methodId,
                    parameters: parameters,
                    usesCancellation: usesCancellation,
                    path: path,
                    verb: input == ProtoInputType.Query ? "Get" : "Post",
                    input: input,
                    output: output,
                    error: opts?.Error ?? match.Error,
                    parameterNaming: parameterNaming,
                    singleJsonParameterWrapping: sjaw,
                    inputDtoTypeName: sjaw switch
                    {
                        ProtoSingleJsonParameterWrapping.DoNotWrap when parameters.Count == 1 => TypeName.Create(parameters[0].Type),
                        _ => TypeName.Create($"Dto{match.Cds.Identifier.ValueText}{methodId}Args")
                    }
                );
            })
            .ToList();

        return new ProtoServiceInfo(
            target: match.TargetType,
            path: rootPath,
            methods: methods
        );
    }
}