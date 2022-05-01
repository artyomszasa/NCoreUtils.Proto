using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoClientInfo
{
    public ITypeSymbol InterfaceType => Service.Target;

    public string InterfaceFullName => InterfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public ITypeSymbol InfoType { get; }

    public ITypeSymbol? JsonSerializerContextType { get; }

    public ProtoServiceInfo Service { get; }

    public string HttpClientConfiguration { get; }

    public ProtoClientInfo(ITypeSymbol infoType, ITypeSymbol? jsonSerializerContextType, ProtoServiceInfo service, string httpClientConfiguration)
    {
        InfoType = infoType;
        JsonSerializerContextType = jsonSerializerContextType;
        Service = service;
        HttpClientConfiguration = httpClientConfiguration;
    }
}