namespace NCoreUtils.Proto.Unit;

[ProtoInfo(typeof(IMath), Input = InputType.Form)]
[ProtoMethodInfo(nameof(IMath.OverrideNumAsync), Input = InputType.Json)]
public partial class FMathInfo { }