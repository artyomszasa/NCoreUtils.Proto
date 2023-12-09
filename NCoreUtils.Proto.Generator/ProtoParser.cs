using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal abstract class ProtoParser
{
    protected static bool Eqx(ISymbol a, ISymbol b) => SymbolEqualityComparer.Default.Equals(a, b);

    protected SemanticModel SemanticModel { get; }

    protected ITypeSymbol TypeCancellationToken { get; }

    protected ITypeSymbol TypeTask { get; }

    protected ITypeSymbol TypeValueTask { get; }

    protected ITypeSymbol TypeTaskOf { get; }

    protected ITypeSymbol TypeValueTaskOf { get; }

    protected Compilation Compilation => SemanticModel.Compilation;

    protected ProtoParser(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
        TypeCancellationToken = GetTypeSymbolForType(typeof(CancellationToken));
        TypeTask = GetTypeSymbolForType(typeof(Task));
        TypeValueTask = GetTypeSymbolForType(typeof(ValueTask));
        TypeTaskOf = ((INamedTypeSymbol)GetTypeSymbolForType(typeof(Task<int>))).ConstructedFrom;
        TypeValueTaskOf = ((INamedTypeSymbol)GetTypeSymbolForType(typeof(ValueTask<int>))).ConstructedFrom;
    }

    protected ITypeSymbol? GetTypeSymbolForTypeOrNull(Type type)
    {
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var elementTypeSymbol = Compilation.GetTypeByMetadataName(elementType.FullName);
            if (elementTypeSymbol is null)
            {
                return default;
            }
            return Compilation.CreateArrayTypeSymbol(elementTypeSymbol);
        }

        if (!type.IsConstructedGenericType)
        {
            return Compilation.GetTypeByMetadataName(type.FullName);
        }

        // get all typeInfo's for the Type arguments
        var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(GetTypeSymbolForTypeOrNull);

        var openType = type.GetGenericTypeDefinition();
        var typeSymbol = Compilation.GetTypeByMetadataName(openType.FullName);
        if (typeSymbol is null || typeArgumentsTypeInfos.Any(ty => ty is null))
        {
            return default;
        }
        return typeSymbol.Construct(typeArgumentsTypeInfos!.ToArray<ITypeSymbol>());
    }

    protected ITypeSymbol GetTypeSymbolForType(Type type)
        => GetTypeSymbolForTypeOrNull(type) ?? throw new InvalidOperationException($"Unable to get type symbol for {type}.");

    protected (bool usesCancellation, IReadOnlyList<ParameterDescriptor>) GetParameters(IMethodSymbol targetMethod, ProtoNaming? naming)
    {
        var parameters = new List<ParameterDescriptor>(targetMethod.Parameters.Length);
        var usesCancellation = false;
        var lastIndex = targetMethod.Parameters.Length - 1;
        for (var i = 0; i < targetMethod.Parameters.Length; ++i)
        {
            var p = targetMethod.Parameters[i];
            if (i == lastIndex && Eqx(TypeCancellationToken, p.Type))
            {
                usesCancellation = true;
            }
            else
            {
                ITypeSymbol? converterType = null;
                var arg0 = p.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "ProtoJsonConverterAttribute")
                    ?.ConstructorArguments.FirstOrDefault();
                if (arg0 is TypedConstant arg)
                {
                    if (arg.Value is not INamedTypeSymbol ctype)
                    {
                        throw new InvalidOperationException($"converterValue.Value is {arg.Value?.GetType()}");
                    }
                    converterType = ctype;
                }
                parameters.Add(new ParameterDescriptor(
                    name: p.Name,
                    key: naming switch
                    {
                        ProtoNaming.CamelCase => NamingConvention.CamelCase.Apply(p.Name),
                        ProtoNaming.KebabCase => NamingConvention.KebabCase.Apply(p.Name),
                        ProtoNaming.PascalCase => NamingConvention.PascalCase.Apply(p.Name),
                        ProtoNaming.SnakeCase => NamingConvention.SnakeCase.Apply(p.Name),
                        _ => p.Name

                    },
                    type: p.Type,
                    typeName: p.Type.ToFullMaybeNullableName(),
                    converterType: converterType
                ));
            }
        }
        return (usesCancellation, parameters);
    }
}
