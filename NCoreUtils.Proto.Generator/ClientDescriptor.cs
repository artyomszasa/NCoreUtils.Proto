using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ClientDescriptor(string @namespace, string name, ITypeSymbol target, IReadOnlyList<MethodDescriptor> methods, string httpClientConfiguration)
{
    public string Namespace { get; } = @namespace;

    public string Name { get; } = name;

    public ITypeSymbol Target { get; } = target;

    public string TargetFullName => Target.ToFullMaybeNullableName();

    public IReadOnlyList<MethodDescriptor> Methods { get; } = methods;

    public string HttpClientConfiguration { get; } = httpClientConfiguration;
}