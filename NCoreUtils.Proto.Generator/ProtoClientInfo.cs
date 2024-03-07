using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoClientInfo(ITypeSymbol infoType, ITypeSymbol? jsonSerializerContextType, ProtoServiceInfo service, string httpClientConfiguration)
{
    public ITypeSymbol InterfaceType => Service.Target;

    public string InterfaceFullName => InterfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public ITypeSymbol InfoType { get; } = infoType;

    public ITypeSymbol? JsonSerializerContextType { get; } = jsonSerializerContextType;

    public ProtoServiceInfo Service { get; } = service;

    public string HttpClientConfiguration { get; } = httpClientConfiguration;
}