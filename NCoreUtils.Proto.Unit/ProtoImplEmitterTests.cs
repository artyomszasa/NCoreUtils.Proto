using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace NCoreUtils.Proto.Unit;

[JsonSerializable(typeof(JsonRootImplEmitterInfo))]
public partial class ImplEmitterJsonContext : JsonSerializerContext { }

[ProtoService(typeof(ImplEmitterInfo), typeof(ImplEmitterJsonContext), ImplementationFactory = typeof(ImplEmitterFactory))]
public partial class ImplEmitterService : IImplEmitterFixture
{
    public CancellationToken LastToken { get; private set; }
    public int VoidCalls { get; private set; }
    public Exception? QuerySingleException { get; set; }

    public Task<int?> JsonValueAsync(int? value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
    public Task<string?> JsonNullableAsync(string? value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
    public Task<string> JsonRequiredAsync(string value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
    public Task<int> JsonWrappedAsync(int value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
    public Task<int> FormSingleAsync(int value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
    public Task<int> FormWrappedAsync(int value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
    public Task<int> FormMultiAsync(int a, int b, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(a + b); }
    public Task<string> FormTextAsync(string text, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(text); }
    public Task<int> QuerySingleAsync(int value, CancellationToken cancellationToken)
    {
        LastToken = cancellationToken;
        if (QuerySingleException is not null) throw QuerySingleException;
        return Task.FromResult(value);
    }
    public Task<int> QueryWrappedAsync(int value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
    public Task<int> QueryMultiAsync(int a, int b, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(a + b); }
    public Task<int> QueryNoneAsync(CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(42); }
    public Task VoidAsync(CancellationToken cancellationToken) { LastToken = cancellationToken; ++VoidCalls; return Task.CompletedTask; }
    public Task<string> CustomAsync(string value, CancellationToken cancellationToken) { LastToken = cancellationToken; return Task.FromResult(value); }
}

public static class ImplEmitterFactory
{
    public static int Calls { get; set; }

    public static ProtoImplEmitterServiceImplementation CreateService(IServiceProvider provider)
    {
        ++Calls;
        return ActivatorUtilities.CreateInstance<ProtoImplEmitterServiceImplementation>(provider);
    }
}

public partial class ProtoImplEmitterServiceImplementation
{
    public bool PassException { get; set; }
    public CancellationToken SeenToken { get; private set; }
    public Exception? SeenException { get; private set; }
    public HttpContext? SeenContext { get; private set; }

    protected int ReadArgumentOfInt32(string? input) => int.Parse(input ?? "0") + 100;

    protected ValueTask<string> ReadCustomRequestAsync(HttpRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Headers["X-Custom"].ToString());

    protected bool ShouldPassException(CancellationToken cancellationToken, Exception exn, HttpContext httpContext)
    {
        SeenToken = cancellationToken;
        SeenException = exn;
        SeenContext = httpContext;
        return PassException;
    }
}

public sealed class ImplEmitterProbe(IImplEmitterFixture service) : ProtoImplEmitterServiceImplementation(service)
{
    public Exception? WrittenError { get; private set; }

    public ValueTask<int?> ReadJsonValue(HttpRequest request, CancellationToken token) => ReadJsonValueRequestAsync(request, token);
    public ValueTask<string?> ReadJsonNullable(HttpRequest request, CancellationToken token) => ReadJsonNullableRequestAsync(request, token);
    public ValueTask<string> ReadJsonRequired(HttpRequest request, CancellationToken token) => ReadJsonRequiredRequestAsync(request, token);
    public ValueTask<DtoImplEmitterInfoJsonWrappedArgs> ReadJsonWrapped(HttpRequest request, CancellationToken token) => ReadJsonWrappedRequestAsync(request, token);
    public Task<int> ReadFormSingle(HttpRequest request, CancellationToken token) => ReadFormSingleRequestAsync(request, token);
    public Task<DtoImplEmitterInfoFormWrappedArgs> ReadFormWrapped(HttpRequest request, CancellationToken token) => ReadFormWrappedRequestAsync(request, token);
    public Task<DtoImplEmitterInfoFormMultiArgs> ReadFormMulti(HttpRequest request, CancellationToken token) => ReadFormMultiRequestAsync(request, token);
    public Task<string> ReadFormText(HttpRequest request, CancellationToken token) => ReadFormTextRequestAsync(request, token);
    public ValueTask<int> ReadQuerySingle(HttpRequest request, CancellationToken token) => ReadQuerySingleRequestAsync(request, token);
    public ValueTask<DtoImplEmitterInfoQueryWrappedArgs> ReadQueryWrapped(HttpRequest request, CancellationToken token) => ReadQueryWrappedRequestAsync(request, token);
    public ValueTask<DtoImplEmitterInfoQueryMultiArgs> ReadQueryMulti(HttpRequest request, CancellationToken token) => ReadQueryMultiRequestAsync(request, token);

    protected override Task WriteQuerySingleErrorAsync(Microsoft.Extensions.Logging.ILogger logger, HttpResponse response, Exception exn, CancellationToken cancellationToken)
    {
        WrittenError = exn;
        return Task.CompletedTask;
    }
}

public sealed class ImplTestEndpointRouteBuilder(IServiceProvider services) : IEndpointRouteBuilder
{
    public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
    public IServiceProvider ServiceProvider { get; } = services;
    public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
}

public class ProtoImplEmitterTests
{
    private static DefaultHttpContext Context(string? body = null, string? contentType = null, string? query = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body ?? string.Empty));
        context.Response.Body = new MemoryStream();
        if (contentType is not null) context.Request.ContentType = contentType;
        if (query is not null) context.Request.QueryString = new QueryString(query);
        return context;
    }

    private static string Route(Endpoint endpoint)
        => string.Join("/", ((RouteEndpoint)endpoint).RoutePattern.PathSegments
            .Select(segment => ((RoutePatternLiteralPart)segment.Parts.Single()).Content));

    [Fact]
    public async Task ReadersPreserveJsonFormAndQueryBranches()
    {
        var probe = new ImplEmitterProbe(new ImplEmitterService());
        Assert.Null(await probe.ReadJsonValue(Context("null").Request, CancellationToken.None));
        Assert.Null(await probe.ReadJsonNullable(Context("null").Request, CancellationToken.None));
        await Assert.ThrowsAsync<ProtoException>(async () => await probe.ReadJsonRequired(Context("null").Request, CancellationToken.None));
        Assert.Equal(7, (await probe.ReadJsonWrapped(Context("{\"value\":7}").Request, CancellationToken.None)).value);

        const string form = "value=2&a=3&b=4&text=hello";
        const string formType = "application/x-www-form-urlencoded";
        Assert.Equal(102, await probe.ReadFormSingle(Context(form, formType).Request, CancellationToken.None));
        Assert.Equal(102, (await probe.ReadFormWrapped(Context(form, formType).Request, CancellationToken.None)).value);
        var multi = await probe.ReadFormMulti(Context(form, formType).Request, CancellationToken.None);
        Assert.Equal((103, 104), (multi.a, multi.b));
        Assert.Equal("hello", await probe.ReadFormText(Context(form, formType).Request, CancellationToken.None));

        const string query = "?value=2&a=3&b=4";
        Assert.Equal(2, await probe.ReadQuerySingle(Context(query: query).Request, CancellationToken.None));
        Assert.Equal(2, (await probe.ReadQueryWrapped(Context(query: query).Request, CancellationToken.None)).value);
        var queryMulti = await probe.ReadQueryMulti(Context(query: query).Request, CancellationToken.None);
        Assert.Equal((3, 4), (queryMulti.a, queryMulti.b));
    }

    [Fact]
    public async Task InvocationPreservesResultVoidCancellationAndErrors()
    {
        var service = new ImplEmitterService();
        var probe = new ImplEmitterProbe(service);
        using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        using var source = new CancellationTokenSource();
        var context = Context(query: "?value=5");
        context.RequestServices = provider;
        context.RequestAborted = source.Token;

        await probe.InvokeQuerySingleAsync(context);
        Assert.Equal(source.Token, service.LastToken);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        context.Response.Body.Position = 0;
        Assert.Equal("5", await new StreamReader(context.Response.Body).ReadToEndAsync());

        var noArgs = Context();
        noArgs.RequestServices = provider;
        noArgs.RequestAborted = source.Token;
        await probe.InvokeQueryNoneAsync(noArgs);
        Assert.Equal(source.Token, service.LastToken);
        Assert.Equal("application/json; charset=utf-8", noArgs.Response.ContentType);
        var voidContext = Context();
        voidContext.RequestServices = provider;
        voidContext.RequestAborted = source.Token;
        await probe.InvokeVoidAsync(voidContext);
        Assert.Equal(1, service.VoidCalls);
        Assert.Null(voidContext.Response.ContentType);
        Assert.Null(typeof(ProtoImplEmitterServiceImplementation).GetMethod("WriteVoidResultAsync",
            BindingFlags.Instance | BindingFlags.NonPublic));

        var customContext = Context();
        customContext.RequestServices = provider;
        customContext.RequestAborted = source.Token;
        customContext.Request.Headers["X-Custom"] = "via-header";
        await probe.InvokeCustomAsync(customContext);
        Assert.Equal(source.Token, service.LastToken);
        customContext.Response.Body.Position = 0;
        Assert.Equal("\"via-header\"", await new StreamReader(customContext.Response.Body).ReadToEndAsync());

        var formError = Context("value=invalid", "application/x-www-form-urlencoded");
        formError.RequestServices = provider;
        await probe.InvokeFormSingleAsync(formError);
        Assert.Equal(500, formError.Response.StatusCode);
        formError.Response.Body.Position = 0;
        Assert.Contains("generic_error", await new StreamReader(formError.Response.Body).ReadToEndAsync());

        var error = new InvalidOperationException("expected");
        service.QuerySingleException = error;
        await probe.InvokeQuerySingleAsync(context);
        Assert.Same(error, probe.WrittenError);
        Assert.Same(error, probe.SeenException);
        Assert.Same(context, probe.SeenContext);
        Assert.Equal(source.Token, probe.SeenToken);
        probe.PassException = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => probe.InvokeQuerySingleAsync(context));
    }

    [Fact]
    public async Task EndpointsPreservePathsMetadataConventionsCachingAndFactory()
    {
        using var provider = new ServiceCollection().AddLogging().AddSingleton<IImplEmitterFixture, ImplEmitterService>().BuildServiceProvider();
        var routeBuilder = new ImplTestEndpointRouteBuilder(provider);
        var conventions = routeBuilder.MapImplEmitterService();
        var source = Assert.IsType<ProtoImplEmitterServiceImplementationDataSource>(Assert.Single(routeBuilder.DataSources));
        Assert.Same(source, conventions);
        Assert.Equal(14, source.Endpoints.Count);
        Assert.Same(source.Endpoints, source.Endpoints);
        Assert.Same(NullChangeToken.Singleton, source.GetChangeToken());
        Assert.Equal("root/part/query-custom", Route(source.Endpoints[(int)ImplEmitterService.Methods.QueryMulti]));
        Assert.Equal("IImplEmitterFixture.QueryMulti", source.Endpoints[(int)ImplEmitterService.Methods.QueryMulti].DisplayName);
        Assert.Equal("PUT", Assert.Single(source.Endpoints[(int)ImplEmitterService.Methods.QueryMulti].Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods));
        Assert.Null(source.Endpoints[(int)ImplEmitterService.Methods.QuerySingle].Metadata.GetMetadata<IHttpMethodMetadata>());

        var overrideSource = new ProtoImplEmitterServiceImplementationDataSource("/ v2 / alt /");
        overrideSource.Add(builder => builder.DisplayName = "global");
        overrideSource.Add(ImplEmitterService.Methods.QueryMulti, builder => builder.DisplayName = "method");
        Assert.Equal("v2/alt/query-custom", Route(overrideSource.Endpoints[(int)ImplEmitterService.Methods.QueryMulti]));
        Assert.Equal("method", overrideSource.Endpoints[(int)ImplEmitterService.Methods.QueryMulti].DisplayName);
        Assert.Equal("global", overrideSource.Endpoints[(int)ImplEmitterService.Methods.QuerySingle].DisplayName);

        ImplEmitterFactory.Calls = 0;
        var context = Context();
        context.RequestServices = provider;
        await source.Endpoints[(int)ImplEmitterService.Methods.QueryNone].RequestDelegate!(context);
        Assert.Equal(1, ImplEmitterFactory.Calls);

        var math = new MathService();
        using var defaultProvider = new ServiceCollection().AddLogging().AddSingleton<IMath>(math).BuildServiceProvider();
        var defaultSource = new ProtoMathServiceImplementationDataSource(null);
        var defaultContext = Context();
        defaultContext.RequestServices = defaultProvider;
        await defaultSource.Endpoints[(int)MathService.Methods.Inc].RequestDelegate!(defaultContext);
        Assert.Equal(1, (await math.OverrideNumAsync(new MyData(0, "x"), CancellationToken.None))!.Num);
    }

    [Fact]
    public async Task DefaultCancellationPredicatePassesCanceledRequestThrough()
    {
        var implementation = new ProtoMathServiceImplementation(new MathService());
        using var source = new CancellationTokenSource();
        source.Cancel();
        var context = Context("{\"a\":1,\"b\":2}");
        context.RequestAborted = source.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => implementation.InvokeAddAsync(context));
    }
}
