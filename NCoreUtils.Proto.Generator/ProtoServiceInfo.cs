using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoServiceInfo(ITypeSymbol target, string path, IReadOnlyList<MethodDescriptor> methods)
{
    public ITypeSymbol Target { get; } = target ?? throw new ArgumentNullException(nameof(target));

    public string TargetFullName => Target.ToFullMaybeNullableName();

    public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

    public IReadOnlyList<MethodDescriptor> Methods { get; } = methods ?? throw new ArgumentNullException(nameof(methods));
}