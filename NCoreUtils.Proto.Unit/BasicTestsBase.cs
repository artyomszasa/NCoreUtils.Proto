using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NCoreUtils.Proto.Unit;

public abstract class BasicTestsBase<TStartup> : IAsyncDisposable
    where TStartup : class
{
    protected IHost TestHost { get; }

    protected ServiceProvider ServiceProvider { get; }

    protected BasicTestsBase(Action<IServiceCollection> configureClientServices)
    {
        TestHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(b => b
                .UseTestServer()
                .UseStartup<TStartup>()
            )
            .Build();
        TestHost.Start();
        var services = new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(() => TestHost!.GetTestClient()));
        configureClientServices(services);
        ServiceProvider = services.BuildServiceProvider(true);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await ServiceProvider.DisposeAsync();
        await TestHost.StopAsync();
    }
}