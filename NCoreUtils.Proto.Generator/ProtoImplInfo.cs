using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoImplInfo
{
    public ITypeSymbol ImplType { get; }

    public ITypeSymbol InterfaceType => Service.Target;

    public string InterfaceFullName => InterfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public ITypeSymbol InfoType { get; }

    public ITypeSymbol? JsonSerializerContextType { get; }

    public ITypeSymbol? ImplementationFactory { get; }

    public ProtoServiceInfo Service { get; }

    public ProtoImplInfo(
        ITypeSymbol implType,
        ITypeSymbol infoType,
        ITypeSymbol? jsonSerializerContextType,
        ITypeSymbol? implementationFactory,
        ProtoServiceInfo service)
    {
        ImplType = implType;
        InfoType = infoType;
        JsonSerializerContextType = jsonSerializerContextType;
        ImplementationFactory = implementationFactory;
        Service = service;
    }
}