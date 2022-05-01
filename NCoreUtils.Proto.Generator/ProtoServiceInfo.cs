using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoServiceInfo
{
    public ITypeSymbol Target { get; }

    public string TargetFullName => Target.ToFullMaybeNullableName();

    public string Path { get; }

    public IReadOnlyList<MethodDescriptor> Methods { get; }

    public ProtoServiceInfo(ITypeSymbol target, string path, IReadOnlyList<MethodDescriptor> methods)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Methods = methods ?? throw new ArgumentNullException(nameof(methods));
    }
}