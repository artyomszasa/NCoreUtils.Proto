using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Proto.Unit;

public class MathStartup
{
    [SuppressMessage("Style", "CA1822")]
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddSingleton<IMath, MathService>();
    }

    [SuppressMessage("Style", "CA1822")]
    public void Configure(IApplicationBuilder app)
    {
        app
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapMathService();
            });
    }
}