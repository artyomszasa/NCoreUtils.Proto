using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public class ParameterDescriptor(string name, string key, ITypeSymbol type, string typeName, ITypeSymbol? converterType)
    : IEquatable<ParameterDescriptor>
{
    public string Name { get; } = name;

    public string Key { get; } = key;

    [Obsolete("Should only be used during parsing")]
    public ITypeSymbol Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    public string TypeInfoPropertyName { get; } = Proto.TypeName.GetTypeInfoPropertyName(type);

    public bool IsValueType { get; } = type.IsValueType;

    public string TypeName { get; } = typeName;

    // private ITypeSymbol? ConverterType { get; } = converterType;

    public string? ConverterTypeFullName { get; } = converterType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public bool Equals([NotNullWhen(true)] ParameterDescriptor? other)
        => other is not null
            && Name == other.Name
            && Key == other.Key
            && TypeInfoPropertyName == other.TypeInfoPropertyName
            && IsValueType == other.IsValueType
            && TypeName == other.TypeName
            && ConverterTypeFullName == other.ConverterTypeFullName;

    public override bool Equals([NotNullWhen(true)] object obj)
        => Equals(obj as ParameterDescriptor);

    public override int GetHashCode() => HashCode.Combine(
        StringComparer.InvariantCulture.GetHashCode(Name),
        StringComparer.InvariantCulture.GetHashCode(Key),
        StringComparer.InvariantCulture.GetHashCode(TypeName),
        StringComparer.InvariantCulture.GetHashCode(ConverterTypeFullName)
    );
}