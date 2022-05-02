using System.Text.Json.Serialization;

namespace NCoreUtils.Proto.Unit;

[JsonSerializable(typeof(JsonRootQMathInfo))]
public partial class QMathJsonSerializerContext : JsonSerializerContext { }