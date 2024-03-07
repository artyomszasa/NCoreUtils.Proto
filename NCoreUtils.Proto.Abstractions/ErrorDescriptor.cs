using System.Text.Json.Serialization;

namespace NCoreUtils.Proto;

public class ErrorDescriptor(string errorCode, string? errorDescription = default)
{
    [JsonPropertyName("error")]
    public string ErrorCode { get; } = errorCode;

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; } = errorDescription;
}