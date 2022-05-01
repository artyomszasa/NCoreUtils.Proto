using System.Text.Json.Serialization;

namespace NCoreUtils.Proto.Unit;

[JsonSerializable(typeof(JsonRootIMath))]
public partial class MathJsonSerializerContext : JsonSerializerContext { }