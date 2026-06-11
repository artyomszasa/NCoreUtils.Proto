namespace NCoreUtils.Proto.Unit;

[ProtoInfo(typeof(IMath), Input = InputType.Query)]
[ProtoMethodInfo(nameof(IMath.OverrideNumAsync), Input = InputType.Json)]
[ProtoMethodInfo(nameof(IMath.IncAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.DoNotWrap)]
[ProtoMethodInfo(nameof(IMath.IncValueWAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
public partial class QMathInfo { }