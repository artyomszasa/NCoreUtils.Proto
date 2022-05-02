using System.Text.Json.Serialization;

namespace NCoreUtils.Proto.Unit;

[JsonSerializable(typeof(JsonRootFMathInfo))]
public partial class FMathJsonSerializerContext : JsonSerializerContext { }