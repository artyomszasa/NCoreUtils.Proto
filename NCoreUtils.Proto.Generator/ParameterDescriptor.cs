using System;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public class ParameterDescriptor(string name, string key, ITypeSymbol type, string typeName, ITypeSymbol? converterType)
{
    public string Name { get; } = name;

    public string Key { get; } = key;

    public ITypeSymbol Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    public string TypeName { get; } = typeName;

    public ITypeSymbol? ConverterType { get; } = converterType;
}