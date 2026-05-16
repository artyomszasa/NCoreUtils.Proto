using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoClientInfo(
    INamedTypeSymbol clientType,
    ITypeSymbol infoType,
    ITypeSymbol? jsonSerializerContextType,
    ProtoServiceInfo service,
    bool noHttpClientFactory,
    string httpClientConfiguration,
    IReadOnlyList<ProtoClientConstructorParameter> additionalConstructorParameters)
{
    public INamedTypeSymbol ClientType => clientType;

    public ITypeSymbol InterfaceType => Service.Target;

    public string InterfaceFullName => InterfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public ITypeSymbol InfoType { get; } = infoType;

    public ITypeSymbol? JsonSerializerContextType { get; } = jsonSerializerContextType;

    public ProtoServiceInfo Service { get; } = service;

    public bool NoHttpClientFactory { get; } = noHttpClientFactory;

    public string HttpClientConfiguration { get; } = httpClientConfiguration;

    public IReadOnlyList<ProtoClientConstructorParameter> AdditionalConstructorParameters { get; } = additionalConstructorParameters;
}