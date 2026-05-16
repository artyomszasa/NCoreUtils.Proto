using System;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public class ProtoClientConstructorParameter(ITypeSymbol type, string name)
{
    public ITypeSymbol Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}
