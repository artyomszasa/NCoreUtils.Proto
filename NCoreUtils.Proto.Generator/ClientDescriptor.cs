using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ClientDescriptor
{
    public string Namespace { get; }

    public string Name { get; }

    public ITypeSymbol Target { get; }

    public string TargetFullName => Target.ToFullMaybeNullableName();

    public IReadOnlyList<MethodDescriptor> Methods { get; }

    public string HttpClientConfiguration { get; }

    public ClientDescriptor(string @namespace, string name, ITypeSymbol target, IReadOnlyList<MethodDescriptor> methods, string httpClientConfiguration)
    {
        Namespace = @namespace;
        Name = name;
        Target = target;
        Methods = methods;
        HttpClientConfiguration = httpClientConfiguration;
    }
}