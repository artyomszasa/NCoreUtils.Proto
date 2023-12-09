using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Proto.Internal;

namespace NCoreUtils.Proto;

public abstract class ProtoClientBase
{
    protected IEndpointConfiguration Configuration { get; }

    protected IHttpClientFactory HttpClientFactory { get; }

    protected abstract string HttpClientConfiguration { get; }

    protected virtual MediaTypeHeaderValue? JsonMediaType => default;

    protected ProtoClientBase(IEndpointConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        Configuration = configuration;
        HttpClientFactory = httpClientFactory;
    }

    protected virtual string StringifyArgument(string value)
        => value;

    protected virtual string StringifyArgument<T>(T value)
        => value?.ToString() ?? string.Empty;

    protected virtual HttpClient CreateHttpClient()
    {
        var client = HttpClientFactory.CreateClient(Configuration.HttpClient ?? HttpClientConfiguration);
        client.BaseAddress = new Uri(Configuration.Endpoint, UriKind.Absolute);
        return client;
    }

    protected virtual async ValueTask HandleErrors(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        try
        {
            var error = await response.Content
                .ReadFromJsonAsync(ErrorDescriptorSerializerContext.Default.ErrorDescriptor, cancellationToken)
                .ConfigureAwait(false);
            if (error is null)
            {
                throw new ProtoException("generic_error", $"Remote server responded with {response.StatusCode} without content [{response.RequestMessage?.RequestUri}].");
            }
            throw new ProtoException(
                string.IsNullOrEmpty(error.ErrorCode) ? "generic_error" : error.ErrorCode,
                error.ErrorDescription ?? $"Remote server responded with {response.StatusCode}."
            );
        }
        catch (Exception exn)
        {
            if (exn is ProtoException)
            {
                throw;
            }
            throw new ProtoException("generic_error", $"Unable to read error response [{response.RequestMessage?.RequestUri}].", exn);
        }
    }
}