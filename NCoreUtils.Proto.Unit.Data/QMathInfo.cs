namespace NCoreUtils.Proto.Unit;

[ProtoInfo(typeof(IMath), Input = InputType.Query)]
[ProtoMethodInfo(nameof(IMath.OverrideNumAsync), Input = InputType.Json)]
public partial class QMathInfo { }