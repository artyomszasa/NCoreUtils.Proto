using System;
using Microsoft.AspNetCore.Builder;
using NCoreUtils.Proto.Internal;

namespace NCoreUtils.AspNetCore;

public interface IProtoEndpointsConventionBuilder<TMethods> : IEndpointConventionBuilder
{
    void Add(TMethods method, Action<EndpointBuilder> convention);

    IProtoEndpointsConventionBuilder<TMethods> ConfigureMethod(TMethods method, Action<IEndpointConventionBuilder> configure)
    {
        var methodBuilder = new ProtoMethodConventionBuilder<TMethods>(this, method);
        configure(methodBuilder);
        return this;
    }
}