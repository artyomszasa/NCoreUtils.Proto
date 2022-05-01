using System;
using System.Net.Http;

namespace NCoreUtils.Proto.Unit;

public class TestHttpClientFactory : IHttpClientFactory
{
    private Func<HttpClient> Factory { get; }

    public TestHttpClientFactory(Func<HttpClient> factory)
    {
        Factory = factory;
    }

    public HttpClient CreateClient(string name)
        => Factory();
}