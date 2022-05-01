using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Proto;
using NCoreUtils.Proto.Internal;

namespace NCoreUtils.AspNetCore;

public abstract class ProtoImplementationBase
{
    protected virtual T ReadArgument<T>(string input)
        => input is null ? default! : (T)Convert.ChangeType(input, typeof(T));

    protected virtual Task WriteErrorAsync(ILogger logger, HttpResponse response, Exception exn, IStatusCodeResponse status, CancellationToken cancellationToken)
    {
        return Task.FromException(exn);
    }

    protected virtual Task WriteErrorAsync(ILogger logger, HttpResponse response, Exception exn, CancellationToken cancellationToken)
    {
        if (exn is IStatusCodeResponse status)
        {
            return WriteErrorAsync(logger, response, exn, status, cancellationToken);
        }
        response.StatusCode = 500;
        logger.LogError(exn, "Proto operation ha failed.");
        return JsonSerializer.SerializeAsync(response.Body, new ErrorDescriptor("generic_error", exn.Message), ErrorDescriptorSerializerContext.Default.ErrorDescriptor, cancellationToken);
    }
}