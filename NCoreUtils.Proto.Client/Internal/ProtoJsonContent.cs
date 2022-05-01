using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace NCoreUtils.Proto.Internal;

public static class ProtoJsonContent
{
    public static ProtoJsonContent<T> Create<T>(T value, JsonTypeInfo<T> typeInfo)
        => new(value, typeInfo);
}

public class ProtoJsonContent<T> : HttpContent
{
    public JsonTypeInfo<T> TypeInfo { get; }

    public T Value { get; }

    public ProtoJsonContent(T value, JsonTypeInfo<T> typeInfo)
    {
        TypeInfo = typeInfo;
        Value = value;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => JsonSerializer.SerializeAsync(stream, Value, TypeInfo);

    protected override bool TryComputeLength(out long length)
    {
        length = default;
        return false;
    }
}