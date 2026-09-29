using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Proto.Unit;

public interface IImplEmitterFixture
{
    Task<int?> JsonValueAsync(int? value, CancellationToken cancellationToken);
    Task<string?> JsonNullableAsync(string? value, CancellationToken cancellationToken);
    Task<string> JsonRequiredAsync(string value, CancellationToken cancellationToken);
    Task<int> JsonWrappedAsync(int value, CancellationToken cancellationToken);
    Task<int> FormSingleAsync(int value, CancellationToken cancellationToken);
    Task<int> FormWrappedAsync(int value, CancellationToken cancellationToken);
    Task<int> FormMultiAsync(int a, int b, CancellationToken cancellationToken);
    Task<string> FormTextAsync(string text, CancellationToken cancellationToken);
    Task<int> QuerySingleAsync(int value, CancellationToken cancellationToken);
    Task<int> QueryWrappedAsync(int value, CancellationToken cancellationToken);
    Task<int> QueryMultiAsync(int a, int b, CancellationToken cancellationToken);
    Task<int> QueryNoneAsync(CancellationToken cancellationToken);
    Task VoidAsync(CancellationToken cancellationToken);
    Task<string> CustomAsync(string value, CancellationToken cancellationToken);
}

[ProtoInfo(typeof(IImplEmitterFixture), Path = "root/part")]
[ProtoMethodInfo(nameof(IImplEmitterFixture.JsonWrappedAsync), SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.FormSingleAsync), Input = InputType.Form)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.FormWrappedAsync), Input = InputType.Form, SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.FormMultiAsync), Input = InputType.Form)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.FormTextAsync), Input = InputType.Form)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.QuerySingleAsync), Input = InputType.Query)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.QueryWrappedAsync), Input = InputType.Query, SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.QueryMultiAsync), Input = InputType.Query, Path = "query-custom", HttpMethod = HttpMethod.Put)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.QueryNoneAsync), Input = InputType.Query)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.VoidAsync), Input = InputType.Query)]
[ProtoMethodInfo(nameof(IImplEmitterFixture.CustomAsync), Input = InputType.Custom)]
public partial class ImplEmitterInfo { }
