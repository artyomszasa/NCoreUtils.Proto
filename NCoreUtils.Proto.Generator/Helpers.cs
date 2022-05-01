using System;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal static class Helpers
{
    private static SymbolDisplayFormat FullyQualifiedMaybeNullFormat { get; } = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
        SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    public static int? UnboxInt32(this object? boxed) => boxed switch
    {
        null => default(int?),
        int i => i,
        _ => default
    };

    public static T? UnboxEnum<T>(this object? boxed) => boxed.UnboxInt32() switch
    {
        int i => (T?)Enum.ToObject(typeof(T), i),
        _ => default
    };

    public static string ToFullMaybeNullableName(this ITypeSymbol type)
    {
        return type.ToDisplayString(FullyQualifiedMaybeNullFormat);
    }
}