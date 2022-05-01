using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Proto.Unit;

public interface IMath
{
    Task<int> AddCAsync(int a, int b, CancellationToken cancellationToken);

    Task<int> AddAsync(int a, int b);

    ValueTask<int> AddVCAsync(int a, int b, CancellationToken cancellationToken);

    ValueTask<int> AddVAsync(int a, int b);

    Task IncAsync(CancellationToken cancellationToken);

    ValueTask<MyData?> OverrideNumAsync(MyData? source, CancellationToken cancellationToken);
}