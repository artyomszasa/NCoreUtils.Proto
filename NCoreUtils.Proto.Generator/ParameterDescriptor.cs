using System;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public class ParameterDescriptor
{
    public string Name { get; }

    public string Key { get; }

    public ITypeSymbol Type { get; }

    public string TypeName { get; }

    public ParameterDescriptor(string name, string key, ITypeSymbol type, string typeName)
    {
        Name = name;
        Key = key;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        TypeName = typeName;
    }
}