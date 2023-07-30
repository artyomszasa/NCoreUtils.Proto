using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Proto.Unit;

[ProtoService(typeof(FMathInfo), typeof(FMathJsonSerializerContext))]
public partial class FMathService : IMath
{
    private int Counter { get; set; }

    public Task<int> AddAsync(int a, int b)
        => Task.FromResult(a + b);

    public Task<int> AddCAsync(int a, int b, CancellationToken cancellationToken)
        => Task.FromResult(a + b);

    public ValueTask<int> AddVAsync(int a, int b)
        => new(a + b);

    public ValueTask<int> AddVCAsync(int a, int b, CancellationToken cancellationToken)
        => new(a + b);

    public Task IncAsync(CancellationToken cancellationToken)
    {
        ++Counter;
        return Task.CompletedTask;
    }

    public ValueTask<MyData?> OverrideNumAsync(MyData? source, CancellationToken cancellationToken)
        => source is null ? default : new(new MyData(Counter, source.Str));
}