using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NCoreUtils.Proto.Unit;

public class FBasicTests : BasicTestsBase<FMathStartup>
{
    public FBasicTests()
        : base(services => services.AddFMathClient("http://localhost"))
    { }

    [Fact]
    public async Task MathDefault()
    {
        var math = ServiceProvider.GetRequiredService<IMath>();
        Assert.Equal(3, await math.AddAsync(1, 2));
        Assert.Equal(3, await math.AddCAsync(1, 2, CancellationToken.None));
        Assert.Equal(3, await math.AddVAsync(1, 2));
        Assert.Equal(3, await math.AddVCAsync(1, 2, CancellationToken.None));
        await math.IncAsync(CancellationToken.None);
        await math.IncAsync(CancellationToken.None);
        var data = await math.OverrideNumAsync(new MyData(12, "abc"), CancellationToken.None);
        Assert.NotNull(data);
        Assert.Equal(2, data!.Num);
        Assert.Equal("abc", data.Str);
    }
}