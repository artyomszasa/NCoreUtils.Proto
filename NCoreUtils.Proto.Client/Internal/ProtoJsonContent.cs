using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace NCoreUtils.Proto.Internal;

public static class ProtoJsonContent
{
    public static ProtoJsonContent<T> Create<T>(T value, JsonTypeInfo<T> typeInfo, MediaTypeHeaderValue? mediaType)
        => new(value, typeInfo, mediaType);
}

public class ProtoJsonContent<T> : HttpContent
{
    private static MediaTypeHeaderValue? Utf8JsonMediaType { get; set; }

    public JsonTypeInfo<T> TypeInfo { get; }

    public T Value { get; }

    public ProtoJsonContent(T value, JsonTypeInfo<T> typeInfo, MediaTypeHeaderValue? mediaType)
    {
        TypeInfo = typeInfo;
        Value = value;
        Headers.ContentType = mediaType ?? (Utf8JsonMediaType ??= new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" });
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => JsonSerializer.SerializeAsync(stream, Value, TypeInfo);

    protected override bool TryComputeLength(out long length)
    {
        length = default;
        return false;
    }
}