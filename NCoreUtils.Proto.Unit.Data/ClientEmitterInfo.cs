using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Proto.Unit;

public interface IClientEmitterFixture
{
    Task<int> JsonAsync(int first, int second, CancellationToken cancellationToken);
    Task<int> SingleAsync(int value, CancellationToken cancellationToken);
    Task<int> WrappedAsync(int value);
    Task<int> FormAsync(string term, int page);
    Task<int> QueryAsync(string term, int page);
    Task<int> CustomAsync(string value, CancellationToken cancellationToken);
    Task VoidAsync(CancellationToken cancellationToken);
}

[ProtoInfo(typeof(IClientEmitterFixture), Path = "api")]
[ProtoMethodInfo(nameof(IClientEmitterFixture.SingleAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.DoNotWrap)]
[ProtoMethodInfo(nameof(IClientEmitterFixture.WrappedAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
[ProtoMethodInfo(nameof(IClientEmitterFixture.FormAsync), Input = InputType.Form)]
[ProtoMethodInfo(nameof(IClientEmitterFixture.QueryAsync), Input = InputType.Query, Path = "query-path")]
[ProtoMethodInfo(nameof(IClientEmitterFixture.CustomAsync), Input = InputType.Custom, Output = OutputType.Custom)]
public partial class ClientEmitterInfo { }
