using System;
using Microsoft.AspNetCore.Builder;
using NCoreUtils.AspNetCore;

namespace NCoreUtils.Proto.Internal;

internal sealed class ProtoMethodConventionBuilder<TMethods> : IEndpointConventionBuilder
{
    private IProtoEndpointsConventionBuilder<TMethods> Builder { get; }

    private TMethods Method { get; }

    public ProtoMethodConventionBuilder(IProtoEndpointsConventionBuilder<TMethods> builder, TMethods method)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Method = method;
    }

    public void Add(Action<EndpointBuilder> convention)
        => Builder.Add(Method, convention);
}