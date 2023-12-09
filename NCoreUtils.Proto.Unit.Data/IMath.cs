using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Globalization;

namespace NCoreUtils.Proto.Unit;

public class CustomIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => int.Parse(reader.GetString() ?? string.Empty, CultureInfo.InvariantCulture);

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

public interface IMath
{
    Task<int> AddCAsync(int a, [ProtoJsonConverter(typeof(CustomIntConverter))] int b, CancellationToken cancellationToken);

    Task<int> AddAsync(int a, int b);

    ValueTask<int> AddVCAsync(int a, int b, CancellationToken cancellationToken);

    ValueTask<int> AddVAsync(int a, int b);

    Task IncAsync(CancellationToken cancellationToken);

    ValueTask<MyData?> OverrideNumAsync(MyData? source, CancellationToken cancellationToken);
}