namespace NCoreUtils.Proto.Unit;

[ProtoInfo(typeof(IMath))]
[ProtoMethodInfo(nameof(IMath.IncAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.DoNotWrap)]
[ProtoMethodInfo(nameof(IMath.IncValueWAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
public partial class MathInfo { }