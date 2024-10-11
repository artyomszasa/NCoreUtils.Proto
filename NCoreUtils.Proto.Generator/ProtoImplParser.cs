using System;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoImplParser(SemanticModel semanticModel) : ProtoConsumerParser(semanticModel)
{
    public ProtoImplInfo Parse(ProtoImplMatch match)
    {
        var service = ParseInfoType(match.InfoType, match.Path);

        var sty = SemanticModel.GetDeclaredSymbol(match.Cds);
        if (sty is not ITypeSymbol implType)
        {
            throw new InvalidOperationException($"Unable to get type symbol from class declaration.");
        }
        return new ProtoImplInfo(
            implType: implType,
            infoType: match.InfoType,
            serviceType: match.ServiceType,
            jsonSerializerContextType: match.JsonSerializerContext,
            implementationFactory: match.ImplementationFactory,
            service: service
        );
    }
}