using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoImplEmitter
{
    private ProtoImplInfo Info { get; }

    public ProtoImplEmitter(ProtoImplInfo info)
    {
        Info = info;
    }

    private string EmitRequestReader(MethodDescriptor desc) => desc.Input switch
    {
        ProtoInputType.Json when true == desc.InputDtoTypeName?.IsNullableReference => @$"protected virtual async global::System.Threading.Tasks.ValueTask<{desc.InputDtoTypeName}> Read{desc.MethodId}RequestAsync(global::Microsoft.AspNetCore.Http.HttpRequest request, global::System.Threading.CancellationToken cancellationToken)
        => await global::System.Text.Json.JsonSerializer.DeserializeAsync(request.Body, {Info.JsonSerializerContextType}.Default.{desc.InputDtoTypeName!.JsonContextName}, cancellationToken);",
        ProtoInputType.Json => @$"protected virtual async global::System.Threading.Tasks.ValueTask<{desc.InputDtoTypeName}> Read{desc.MethodId}RequestAsync(global::Microsoft.AspNetCore.Http.HttpRequest request, global::System.Threading.CancellationToken cancellationToken)
        => (await global::System.Text.Json.JsonSerializer.DeserializeAsync(request.Body, {Info.JsonSerializerContextType}.Default.{desc.InputDtoTypeName!.JsonContextName}, cancellationToken))
            ?? throw new global::NCoreUtils.Proto.ProtoException(""generic_error"", ""Unable to deserialize JSON arguments for {Info.InterfaceFullName}.{desc.MethodName}."");",
        ProtoInputType.Form => @$"protected virtual async global::System.Threading.Tasks.Task<{desc.InputDtoTypeName}> Read{desc.MethodId}RequestAsync(global::Microsoft.AspNetCore.Http.HttpRequest request, global::System.Threading.CancellationToken cancellationToken)
        {{
            var data = await request.ReadFormAsync(cancellationToken);
            return new {desc.InputDtoTypeName}(
                {string.Join("," + Environment.NewLine + "                ", desc.Parameters.Select(e => $"ReadArgument<{e.TypeName}>(data[\"{e.Key}\"])"))}
            );
        }}",
        ProtoInputType.Query when desc.Parameters.Count > 0 => @$"protected virtual global::System.Threading.Tasks.ValueTask<{desc.InputDtoTypeName}> Read{desc.MethodId}RequestAsync(global::Microsoft.AspNetCore.Http.HttpRequest request, global::System.Threading.CancellationToken cancellationToken)
        {{
            var data = request.Query;
            return new global::System.Threading.Tasks.ValueTask<{desc.InputDtoTypeName}>(new {desc.InputDtoTypeName}(
                {string.Join("," + Environment.NewLine + "                ", desc.Parameters.Select(e => $"ReadArgument<{e.TypeName}>(data[\"{e.Key}\"])"))}
            ));
        }}",
        _ => string.Empty
    };

    private string EmitErrorWriter(MethodDescriptor desc)
        => @$"protected virtual global::System.Threading.Tasks.Task Write{desc.MethodId}ErrorAsync(global::Microsoft.Extensions.Logging.ILogger logger, global::Microsoft.AspNetCore.Http.HttpResponse response, global::System.Exception exn, global::System.Threading.CancellationToken cancellationToken)
        => WriteErrorAsync(logger, response, exn, cancellationToken);";

    private string EmitResultWriter(MethodDescriptor desc)
        => desc.NoReturn ? string.Empty : desc.Output switch
        {
            ProtoOutputType.Json => @$"protected virtual global::System.Threading.Tasks.Task Write{desc.MethodId}ResultAsync(global::Microsoft.AspNetCore.Http.HttpResponse response, {desc.ReturnValueType} result, global::System.Threading.CancellationToken cancellationToken)
        => global::System.Text.Json.JsonSerializer.SerializeAsync(response.Body, result, {Info.JsonSerializerContextType}.Default.{desc.ReturnValueType.JsonContextName}!, cancellationToken);",
            _ => string.Empty
        };

    private string EmitMethodInvoker(MethodDescriptor desc)
        => @$"protected internal virtual async global::System.Threading.Tasks.Task Invoke{desc.MethodId}Async(global::Microsoft.AspNetCore.Http.HttpContext httpContext)
    {{
        try
        {{
            {(desc.Parameters.Count > 0 ? $"var arguments = await Read{desc.MethodId}RequestAsync(httpContext.Request, httpContext.RequestAborted);" : string.Empty)}
            {(desc.NoReturn ? string.Empty : "var result = ")}await Impl.{desc.MethodName}({(desc.SingleJsonParameterWrapping == ProtoSingleJsonParameterWrapping.DoNotWrap && desc.Parameters.Count == 1 ? "arguments" : string.Join(", ", desc.Parameters.Select(e => $"arguments.{e.Name}")))}{(desc.UsesCancellation ? $"{(desc.Parameters.Count == 0 ? string.Empty : ", ")}httpContext.RequestAborted" : string.Empty)});
            {(desc.NoReturn ? string.Empty : $"await Write{desc.MethodId}ResultAsync(httpContext.Response, result, httpContext.RequestAborted);")}
        }}
        catch (global::System.Exception exn)
        {{
            var logger = global::Microsoft.Extensions.Logging.LoggerFactoryExtensions.CreateLogger<{Info.ImplType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(
                global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Microsoft.Extensions.Logging.ILoggerFactory>(httpContext.RequestServices)
            );
            await Write{desc.MethodId}ErrorAsync(logger, httpContext.Response, exn, httpContext.RequestAborted);
        }}
    }}";

    public string EmitImpl(string @namespace, string name)
    {
        return @$"#nullable enable
namespace {@namespace}
{{
public partial class {name} : global::NCoreUtils.AspNetCore.ProtoImplementationBase
{{
    public {Info.InterfaceFullName} Impl {{ get; }}

    public {name}({Info.InterfaceFullName} impl)
        => Impl = impl ?? throw new global::System.ArgumentNullException(nameof(impl));

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Select(EmitRequestReader))}

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Select(EmitErrorWriter))}

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Select(EmitResultWriter))}

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Select(EmitMethodInvoker))}
}}

public class {name}DataSource : global::Microsoft.AspNetCore.Routing.EndpointDataSource, global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
{{
    private static class Segments
    {{
        public static global::Microsoft.AspNetCore.Routing.Patterns.RoutePattern Combine(global::System.Collections.Generic.IEnumerable<global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment> parentSegments, string rawSegment)
        {{
            var segment = global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Segment(global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.LiteralPart(rawSegment));
            return parentSegments is null
                ? global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Pattern(segment)
                : global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Pattern(
                    global::System.Linq.Enumerable.Append(parentSegments, segment)
                );
        }}
    }}

    private global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.Endpoint>? _endpoints;

    private global::System.Collections.Generic.List<global::System.Action<global::Microsoft.AspNetCore.Builder.EndpointBuilder>> Conventions {{ get; }} = new global::System.Collections.Generic.List<global::System.Action<global::Microsoft.AspNetCore.Builder.EndpointBuilder>>();

    private object Sync {{ get; }} = new object();

    private string? Path {{ get; }}

    public override global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.Endpoint> Endpoints
    {{
        get
        {{
            if (_endpoints is null)
            {{
                lock (Sync)
                {{
                    if (_endpoints is null)
                    {{
                        _endpoints = BuildEndpoints();
                    }}
                }}
            }}
            return _endpoints;
        }}
    }}

    public {name}DataSource(string? path)
        => Path = path;

    private global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.Endpoint> BuildEndpoints()
    {{
        var endpoints = new global::System.Collections.Generic.List<global::Microsoft.AspNetCore.Http.Endpoint>({Info.Service.Methods.Count});
        var servicePathSegments = Path is null
            ? new global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment[]
            {{
                {(string.IsNullOrEmpty(Info.Service.Path) ? string.Empty : string.Join(',' + Environment.NewLine + "                ", Info.Service.Path.Split('/').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Select(s => $"global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Segment(global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.LiteralPart(\"{s}\"))")))}
            }}
            : global::System.Linq.Enumerable.ToArray(
                global::System.Linq.Enumerable.Select(
                    global::System.Linq.Enumerable.Where(
                        global::System.Linq.Enumerable.Select(
                            Path.Split('/'),
                            s => s.Trim()
                        ),
                        s => !string.IsNullOrEmpty(s)
                    ),
                    s => global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Segment(global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.LiteralPart(s))
                )
            );
        {string.Join(Environment.NewLine + "        ", Info.Service.Methods.Select(EmitAddEndpoint))}
        return endpoints;
    }}

    public void Add(global::System.Action<global::Microsoft.AspNetCore.Builder.EndpointBuilder> convention)
        => Conventions.Add(convention);

    public override global::Microsoft.Extensions.Primitives.IChangeToken GetChangeToken()
        => global::Microsoft.Extensions.FileProviders.NullChangeToken.Singleton;
}}

public static class EndpointRouteBuilder{name}Extensions
{{
    public static global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder Map{Info.ImplType.Name}(
        this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
        string? path = default)
    {{
        var dataSource = new {name}DataSource(path);
        endpoints.DataSources.Add(dataSource);
        return dataSource;
    }}
}}
}}";

    string EmitAddEndpoint(MethodDescriptor desc)
        => @$"
        global::Microsoft.AspNetCore.Http.RequestDelegate delegate{desc.MethodId} = httpContext =>
        {{
            var impl = {(Info.ImplementationFactory is null ? $"global::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<{name}>" : $"{Info.ImplementationFactory.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.CreateService")}(httpContext.RequestServices);
            return impl.Invoke{desc.MethodId}Async(httpContext);
        }};
        var builder{desc.MethodId} = new global::Microsoft.AspNetCore.Routing.RouteEndpointBuilder(delegate{desc.MethodId}, Segments.Combine(servicePathSegments, ""{desc.Path}""), 100)
        {{
            DisplayName = ""{Info.InterfaceType.Name}.{desc.MethodId}""
        }};
        foreach (var convention in Conventions)
        {{
            convention(builder{desc.MethodId});
        }}
        endpoints.Add(builder{desc.MethodId}.Build());";
    }
}