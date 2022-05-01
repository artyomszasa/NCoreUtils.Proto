using System.Text.Json.Serialization;

namespace NCoreUtils.Proto;

public class ErrorDescriptor
{
    [JsonPropertyName("error")]
    public string ErrorCode { get; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; }

    public ErrorDescriptor(string errorCode, string? errorDescription = default)
    {
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
    }
}