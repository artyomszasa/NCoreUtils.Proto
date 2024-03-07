using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoClientParser(SemanticModel semanticModel) : ProtoConsumerParser(semanticModel)
{
    public ProtoClientInfo Parse(ProtoClientMatch match)
    {
        var service = ParseInfoType(match.InfoType, match.Path);
        return new ProtoClientInfo(
            infoType: match.InfoType,
            jsonSerializerContextType: match.JsonSerializerContext,
            service: service,
            httpClientConfiguration: match.Cds.Identifier.ValueText
        );
    }
}