using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NCoreUtils.Proto.Unit;

[JsonSerializable(typeof(JsonRootClientEmitterInfo))]
public partial class ClientEmitterJsonContext : JsonSerializerContext { }

public sealed class ClientEmitterDependency(ClientRequestRecorder recorder)
{
    public ClientRequestRecorder Recorder { get; } = recorder;
}

[ProtoClient(typeof(ClientEmitterInfo), typeof(ClientEmitterJsonContext), NoHttpClientFactory = true)]
[ProtoClientConstructorParameter(typeof(ClientEmitterDependency), "dependency")]
public partial class BranchClient
{
    public ClientEmitterDependency CurrentDependency => dependency;

    public string PathFor(Methods method) => GetMethodPath(method);

    public CancellationToken? ErrorToken { get; private set; }

    public CancellationToken? ResponseToken { get; private set; }

    protected override HttpClient CreateHttpClient()
        => new(dependency.Recorder, disposeHandler: false)
        {
            BaseAddress = new Uri(Configuration.Endpoint)
        };

    protected HttpRequestMessage CreateCustomRequest(string value)
        => new(System.Net.Http.HttpMethod.Post, GetMethodPath(Methods.Custom)) { Content = new StringContent(value) };

    protected ValueTask HandleCustomErrors(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ErrorToken = cancellationToken;
        return ValueTask.CompletedTask;
    }

    [HandlesResponseDisposal]
    protected Task<int> ReadCustomResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ResponseToken = cancellationToken;
        return Task.FromResult(42);
    }
}

public sealed class ClientRequestRecorder : HttpMessageHandler
{
    public List<(System.Net.Http.HttpMethod Method, string Uri, string? Body, CancellationToken Token)> Requests { get; } = new();

    public List<TrackedResponseContent> Responses { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((
            request.Method,
            request.RequestUri!.OriginalString,
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
            cancellationToken
        ));
        var content = new TrackedResponseContent("42");
        Responses.Add(content);
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content };
    }
}

public sealed class TrackedResponseContent(string value) : StringContent(value, Encoding.UTF8, "application/json")
{
    public bool WasDisposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WasDisposed = true;
        }
        base.Dispose(disposing);
    }
}

public class ProtoClientEmitterTests
{
    [Fact]
    public async Task RequestsCoverJsonFormQueryCustomVoidAndCancellation()
    {
        using var recorder = new ClientRequestRecorder();
        var dependency = new ClientEmitterDependency(recorder);
        var configuration = new BranchClientConfiguration { Endpoint = "https://example.test/", Path = "/v2/" };
        var client = new BranchClient(configuration, dependency);
        using var source = new CancellationTokenSource();
        var token = source.Token;

        Assert.Equal("v2/query-path", client.PathFor(BranchClient.Methods.Query));
        Assert.Same(dependency, client.CurrentDependency);
        Assert.Equal(42, await client.JsonAsync(1, 2, token));
        Assert.Equal(42, await client.SingleAsync(7, token));
        Assert.Equal(42, await client.WrappedAsync(8));
        Assert.Equal(42, await client.FormAsync("a b", 2));
        Assert.Equal(42, await client.QueryAsync("a b", 2));
        Assert.Equal(42, await client.CustomAsync("payload", token));
        await client.VoidAsync(token);

        Assert.Equal(7, recorder.Requests.Count);
        Assert.Equal(("https://example.test/v2/json", "{\"first\":1,\"second\":2}"), (recorder.Requests[0].Uri, recorder.Requests[0].Body));
        Assert.Equal("7", recorder.Requests[1].Body);
        Assert.Equal("{\"value\":8}", recorder.Requests[2].Body);
        Assert.Equal("term=a+b&page=2", recorder.Requests[3].Body);
        Assert.Equal(System.Net.Http.HttpMethod.Get, recorder.Requests[4].Method);
        Assert.Equal("https://example.test/v2/query-path?term=a%20b&page=2", recorder.Requests[4].Uri);
        Assert.Equal("payload", recorder.Requests[5].Body);
        Assert.Equal(System.Net.Http.HttpMethod.Get, recorder.Requests[6].Method);
        Assert.True(recorder.Requests[0].Token.CanBeCanceled);
        Assert.True(recorder.Requests[5].Token.CanBeCanceled);
        Assert.True(recorder.Requests[6].Token.CanBeCanceled);
        Assert.Equal(token, client.ErrorToken);
        Assert.Equal(token, client.ResponseToken);
        Assert.All(recorder.Responses, response => Assert.True(response.WasDisposed || ReferenceEquals(response, recorder.Responses[5])));
        Assert.False(recorder.Responses[5].WasDisposed);
    }

    [Fact]
    public void RegistrationAndConstructorBranchesCompileAndResolve()
    {
        using var recorder = new ClientRequestRecorder();
        var dependency = new ClientEmitterDependency(recorder);
        var services = new ServiceCollection().AddSingleton(dependency);
        services.AddBranchClient("https://example.test/", path: "/custom/");
        using var provider = services.BuildServiceProvider();
        var client = Assert.IsType<BranchClient>(provider.GetRequiredService<IClientEmitterFixture>());
        Assert.Same(dependency, client.CurrentDependency);
        Assert.Equal("custom/json", client.PathFor(BranchClient.Methods.Json));
        Assert.Equal(typeof(ProtoClientCore), typeof(BranchClient).BaseType);
        Assert.Equal(typeof(ProtoClientBase), typeof(MathClient).BaseType);

        var endpoint = new BranchClientConfiguration { Endpoint = "https://example.test/", Path = "ignored" };
        var other = new ServiceCollection().AddSingleton(dependency);
        other.AddBranchClient((IEndpointConfiguration)endpoint, path: "/override/");
        using var otherProvider = other.BuildServiceProvider();
        Assert.Equal("override/json", Assert.IsType<BranchClient>(otherProvider.GetRequiredService<IClientEmitterFixture>()).PathFor(BranchClient.Methods.Json));

        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Endpoint"] = "https://example.test/",
            ["Path"] = "config"
        }).Build();
        var configured = new ServiceCollection().AddSingleton(dependency);
        configured.AddBranchClient(settings);
        using var configuredProvider = configured.BuildServiceProvider();
        Assert.Equal("config/json", Assert.IsType<BranchClient>(configuredProvider.GetRequiredService<IClientEmitterFixture>()).PathFor(BranchClient.Methods.Json));
    }
}
