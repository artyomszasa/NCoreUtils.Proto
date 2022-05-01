using System.Text.Json.Serialization;

namespace NCoreUtils.Proto.Internal;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ErrorDescriptor))]
public partial class ErrorDescriptorSerializerContext : JsonSerializerContext { }