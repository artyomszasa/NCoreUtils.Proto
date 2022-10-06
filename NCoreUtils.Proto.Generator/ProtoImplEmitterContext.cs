using System;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public sealed class ProtoImplEmitterContext
{
    private static SymbolEqualityComparer Eq { get; } = SymbolEqualityComparer.Default;

    private SemanticModel SemanticModel { get; }

    private ITypeSymbol? _cancellationTokenSymbol;

    private ITypeSymbol? _exceptionSymbol;

    private ITypeSymbol? _httpContextSymbol;

    private ITypeSymbol CancellationTokenSymbol
        => _cancellationTokenSymbol ??= SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken")
            ?? throw new InvalidOperationException("Cannot get type symbol for System.Threading.CancellationToken.");

    private ITypeSymbol ExceptionSymbol
        => _exceptionSymbol ??= SemanticModel.Compilation.GetTypeByMetadataName("System.Exception")
            ?? throw new InvalidOperationException("Cannot get type symbol for System.Exception.");

    private ITypeSymbol HttpContextSymbol
        => _httpContextSymbol ??= SemanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext")
            ?? throw new InvalidOperationException("Cannot get type symbol for Microsoft.AspNetCore.Http.HttpContext.");

    public ProtoImplEmitterContext(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
    }

    public bool IsCancellationToken(ITypeSymbol symbol)
        => Eq.Equals(symbol, CancellationTokenSymbol);

    public bool IsException(ITypeSymbol symbol)
        => Eq.Equals(symbol, ExceptionSymbol);

    public bool IsHttpContext(ITypeSymbol symbol)
        => Eq.Equals(symbol, HttpContextSymbol);

    public bool IsAnyException(ITypeSymbol symbol)
        => !symbol.IsValueType
            && (Eq.Equals(symbol, ExceptionSymbol)
                || (symbol.BaseType is not null
                    && symbol.BaseType.SpecialType != SpecialType.System_Object
                    && IsAnyException(symbol.BaseType)
                )
            );
}