using System;
using Microsoft.AspNetCore.Builder;
using NCoreUtils.AspNetCore;

namespace NCoreUtils.Proto.Internal;

internal sealed class ProtoMethodConventionBuilder<TMethods>(IProtoEndpointsConventionBuilder<TMethods> builder, TMethods method) : IEndpointConventionBuilder
{
    private IProtoEndpointsConventionBuilder<TMethods> Builder { get; } = builder ?? throw new ArgumentNullException(nameof(builder));

    private TMethods Method { get; } = method;

    public void Add(Action<EndpointBuilder> convention)
        => Builder.Add(Method, convention);
}