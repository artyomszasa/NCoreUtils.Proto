using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoImplInfo(
    ITypeSymbol implType,
    INamedTypeSymbol serviceType,
    ITypeSymbol infoType,
    ITypeSymbol? jsonSerializerContextType,
    ITypeSymbol? implementationFactory,
    ProtoServiceInfo service)
{
    public ITypeSymbol ImplType { get; } = implType;

    public ITypeSymbol InterfaceType => Service.Target;

    public string InterfaceFullName => InterfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public INamedTypeSymbol ServiceType { get; } = serviceType;

    public ITypeSymbol InfoType { get; } = infoType;

    public ITypeSymbol? JsonSerializerContextType { get; } = jsonSerializerContextType;

    public ITypeSymbol? ImplementationFactory { get; } = implementationFactory;

    public ProtoServiceInfo Service { get; } = service;
}