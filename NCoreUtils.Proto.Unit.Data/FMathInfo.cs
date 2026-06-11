namespace NCoreUtils.Proto.Unit;

[ProtoInfo(typeof(IMath), Input = InputType.Form)]
[ProtoMethodInfo(nameof(IMath.OverrideNumAsync), Input = InputType.Json, HttpMethod = HttpMethod.Put)]
[ProtoMethodInfo(nameof(IMath.IncAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.DoNotWrap)]
[ProtoMethodInfo(nameof(IMath.IncValueWAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
public partial class FMathInfo { }