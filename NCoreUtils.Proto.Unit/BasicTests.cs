using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace NCoreUtils.Proto.Unit;

public class BasicTests : IAsyncDisposable
{
    private IHost TestHost { get; }

    private ServiceProvider ServiceProvider { get; }

    public BasicTests()
    {
        TestHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(b => b
                .UseTestServer()
                .UseStartup<MathStartup>()
            )
            .Build();
        TestHost.Start();
        ServiceProvider = new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(() => TestHost!.GetTestClient()))
            .AddMathClient("http://localhost")
            .BuildServiceProvider(true);
    }

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

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await ServiceProvider.DisposeAsync();
        await TestHost.StopAsync();
    }
}