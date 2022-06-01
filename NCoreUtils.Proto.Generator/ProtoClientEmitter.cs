using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class ProtoClientEmitter
{
    private ProtoClientInfo Info { get; }

    public ProtoClientEmitter(ProtoClientInfo info)
    {
        Info = info;
    }

    private string EmitCreateJsonContentMethod(MethodDescriptor desc)
        => desc.SingleJsonParameterWrapping switch
        {
            ProtoSingleJsonParameterWrapping.DoNotWrap when desc.Parameters.Count == 1
                => @$"protected virtual global::System.Net.Http.HttpContent Create{desc.MethodId}RequestContent({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))})
    {{
        return global::NCoreUtils.Proto.Internal.ProtoJsonContent.Create({desc.Parameters[0].Name}, {Info.JsonSerializerContextType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Default.{desc.InputDtoTypeName!.JsonContextName}{(desc.InputDtoTypeName.IsNullableReference ? "!" : string.Empty)}, JsonMediaType);
    }}",
            _ => @$"protected virtual global::System.Net.Http.HttpContent Create{desc.MethodId}RequestContent({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))})
    {{
        return global::NCoreUtils.Proto.Internal.ProtoJsonContent.Create(new {desc.InputDtoTypeName}({string.Join(", ", desc.Parameters.Select(e => e.Name))}), {Info.JsonSerializerContextType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Default.{desc.InputDtoTypeName!.JsonContextName}, JsonMediaType);
    }}"
        };

    private string EmitCreateFormContentMethod(MethodDescriptor desc)
    {
        return @$"protected virtual global::System.Net.Http.HttpContent Create{desc.MethodId}RequestContent({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))})
    {{
        var data = new global::System.Collections.Generic.Dictionary<string, string>({desc.Parameters.Count});
        {string.Join(Environment.NewLine + "        ", desc.Parameters.Select(EmitAdd))}
        return new global::System.Net.Http.FormUrlEncodedContent(data);
    }}";

        static string EmitAdd(ParameterDescriptor p)
            => p.Type.IsValueType
                ? $@"if (default != {p.Name})
        {{
            data.Add(""{p.Key}"", StringifyArgument({p.Name}));
        }}"
                : $@"if ({p.Name} is not null)
        {{
            data.Add(""{p.Key}"", StringifyArgument({p.Name}));
        }}";
    }

    private string EmitCreateRequestMethod(MethodDescriptor desc)
        => desc.Input == ProtoInputType.Custom ? string.Empty : desc.Input == ProtoInputType.Query
            ? @$"protected virtual global::System.Net.Http.HttpRequestMessage Create{desc.MethodId}Request({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))})
    {{
        var path = GetCachedMethodPath(Methods.{desc.MethodId})
            {(desc.Parameters.Count == 0 ? string.Empty : "+ ")}{string.Join(Environment.NewLine + "            + ", desc.Parameters.Select((e, i) => $"$\"{(i == 0 ? '?' : '&')}{e.Key}={{Escape(StringifyArgument({e.Name}))}}\""))};
        return new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.{desc.Verb}, path);

        {(desc.Parameters.Count == 0 ? string.Empty : "static string Escape(string? value) => global::System.Uri.EscapeDataString(value ?? string.Empty);")}
    }}"     : @$"protected virtual global::System.Net.Http.HttpRequestMessage Create{desc.MethodId}Request({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))})
    {{
        return new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.{desc.Verb}, GetCachedMethodPath(Methods.{desc.MethodId}))
        {{
            Content = Create{desc.MethodId}RequestContent({string.Join(", ", desc.Parameters.Select(e => e.Name))})
        }};
    }}";

    private string EmitReadResponseMethod(MethodDescriptor desc)
        => desc.NoReturn ? string.Empty : desc.Output switch
        {
            ProtoOutputType.Json => @$"protected virtual {desc.ReturnType} Read{desc.MethodId}Response(global::System.Net.Http.HttpResponseMessage response, global::System.Threading.CancellationToken cancellationToken)
    {{
        return {(desc.AsyncReturnType == AsyncReturnType.ValueTask ? $"new {desc.ReturnType}(" : string.Empty)}global::System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync(response.Content, {Info.JsonSerializerContextType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Default.{desc.ReturnValueType.JsonContextName}, cancellationToken)!{(desc.AsyncReturnType == AsyncReturnType.ValueTask ? $")" : string.Empty)};
    }}",
            ProtoOutputType.Custom => string.Empty,
            var output => throw new InvalidOperationException($"Unsupported ouput type {output}.")
        };

    private string EmitMethod(MethodDescriptor desc)
    {
        var requestVar = "request";
        var requestVarSeed = 0;
        while (desc.Parameters.Any(p => p.Name == requestVar))
        {
            requestVar = $"request{++requestVarSeed}";
        }
        var ctoken = desc.UsesCancellation ? "cancellationToken" : "global::System.Threading.CancellationToken.None";
        return @$"public virtual async {desc.ReturnType} {desc.MethodName}({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))}{(desc.UsesCancellation ? (desc.Parameters.Count > 0 ? ", " : string.Empty) + "global::System.Threading.CancellationToken cancellationToken" : string.Empty)})
    {{
        var {requestVar} = Create{desc.MethodId}Request({string.Join(", ", desc.Parameters.Select(e => e.Name))});
        using var client = CreateHttpClient();
        using var response = await client.SendAsync({requestVar}, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, {ctoken});
        await HandleErrors(response, {ctoken});
        {(desc.NoReturn ? string.Empty : $"return await Read{desc.MethodId}Response(response, {ctoken});")}
    }}";
    }

    public string EmitClient(string @namespace, string name)
        => @$"#nullable enable
namespace {@namespace}
{{
public sealed class {name}Configuration : global::NCoreUtils.Proto.IEndpointConfiguration
{{
    public string? HttpClient {{ get; set; }}

    public string Endpoint {{ get; set; }} = string.Empty;

    public string? Path {{ get; set; }}
}}

public static class ServiceCollection{name}Extensions
{{
    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{name}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, {name}Configuration configuration)
        => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<{Info.InterfaceFullName}, {name}>(
            Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, configuration)
        );

    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{name}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::NCoreUtils.Proto.IEndpointConfiguration configuration, string? path = default)
        => services.Add{name}(new {name}Configuration
        {{
            HttpClient = configuration.HttpClient,
            Endpoint = configuration.Endpoint,
            Path = path
        }});

    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{name}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, string endpoint, string? httpClientConfiguration = default, string? path = default)
        => services.Add{name}(new {name}Configuration
        {{
            HttpClient = httpClientConfiguration,
            Endpoint = endpoint,
            Path = path
        }});

    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{name}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::Microsoft.Extensions.Configuration.IConfiguration configuration)
        => services.Add{name}(new {name}Configuration
        {{
            HttpClient = configuration[""HttpClient""],
            Endpoint = configuration[""Endpoint""],
            Path = configuration[""Path""],
        }});
}}

public partial class {name} : global::NCoreUtils.Proto.ProtoClientBase, {Info.InterfaceFullName}
{{
    public enum Methods {{ {string.Join(", ", Info.Service.Methods.Select(e => e.MethodId))} }}

    private global::System.Collections.Concurrent.ConcurrentDictionary<Methods, string> MethodPathCache {{ get; }} = new global::System.Collections.Concurrent.ConcurrentDictionary<Methods, string>();

    private global::System.Func<Methods, string> MethodPathFactory {{ get; }}

    protected global::System.Collections.Generic.IReadOnlyDictionary<Methods, string> MethodPaths {{ get; }}

    protected string ServicePath {{ get; }} = ""{Info.Service.Path}"";

    protected override string HttpClientConfiguration => ""{Info.HttpClientConfiguration}"";

    public {name}({name}Configuration configuration, global::System.Net.Http.IHttpClientFactory httpClientFactory)
        : base(configuration, httpClientFactory)
    {{
        if (!string.IsNullOrEmpty(configuration.Path))
        {{
            ServicePath = configuration.Path.Trim('/');
        }}
        var methodPaths = new global::System.Collections.Generic.Dictionary<Methods, string>({Info.Service.Methods.Count})
        {{
            {string.Join("," + Environment.NewLine + "            ", Info.Service.Methods.Select(e => $"{{ Methods.{e.MethodId}, \"{e.Path}\" }}"))}
        }};
        MethodPaths = methodPaths;
        MethodPathFactory = GetMethodPath;
    }}

    private string GetCachedMethodPath(Methods method)
        => MethodPathCache.GetOrAdd(method, MethodPathFactory);

    protected virtual string GetMethodPath(Methods method)
        => string.IsNullOrEmpty(ServicePath) ? MethodPaths[method] : $""{{ServicePath}}/{{MethodPaths[method]}}"";

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Where(e => e.Input == ProtoInputType.Json).Select(EmitCreateJsonContentMethod))}

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Where(e => e.Input == ProtoInputType.Form).Select(EmitCreateFormContentMethod))}

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Select(EmitCreateRequestMethod))}

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Select(EmitReadResponseMethod))}

    {string.Join(Environment.NewLine + "    ", Info.Service.Methods.Select(EmitMethod))}
}}
}}";
}