using System;
using System.Net.Http;

namespace NCoreUtils.Proto;

public abstract class ProtoClientBase(IEndpointConfiguration configuration, IHttpClientFactory httpClientFactory)
    : ProtoClientCore(configuration)
{
    protected IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    protected override HttpClient CreateHttpClient()
    {
        var client = HttpClientFactory.CreateClient(Configuration.HttpClient ?? HttpClientConfiguration);
        client.BaseAddress = new Uri(Configuration.Endpoint, UriKind.Absolute);
        return client;
    }
}