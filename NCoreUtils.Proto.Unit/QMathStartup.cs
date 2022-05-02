using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Proto.Unit;

public class QMathStartup
{
    [SuppressMessage("Style", "CA1822")]
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddSingleton<IMath, QMathService>();
    }

    [SuppressMessage("Style", "CA1822")]
    public void Configure(IApplicationBuilder app)
    {
        app
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapQMathService();
            });
    }
}