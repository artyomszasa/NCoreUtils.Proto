using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Proto.Unit;

public class FMathStartup
{
    [SuppressMessage("Style", "CA1822")]
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddSingleton<IMath, FMathService>();
    }

    [SuppressMessage("Style", "CA1822")]
    public void Configure(IApplicationBuilder app)
    {
        app
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapFMathService();
            });
    }
}