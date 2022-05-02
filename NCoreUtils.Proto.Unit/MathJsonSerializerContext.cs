using System.Text.Json.Serialization;

namespace NCoreUtils.Proto.Unit;

[JsonSerializable(typeof(JsonRootMathInfo))]
public partial class MathJsonSerializerContext : JsonSerializerContext { }