using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Proto;

internal class ProtoClientEmitter(ProtoClientInfo info)
{
    private static string NewLine { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "\r\n"
        : "\n";

    private ProtoClientInfo Info { get; } = info;

    private string GetReadResponseMethodName(MethodDescriptor desc)
        => $"Read{desc.MethodId}Response";

    private bool ShouldDisposeResponse(MethodDescriptor desc)
    {
        var methodName = GetReadResponseMethodName(desc);
        return
            !(Info.ClientType
                .GetMembers()
                .TryChooseFirst(methodName, static (mem, name) => mem is IMethodSymbol m && m.Name == name ? m.Choose() : default, out var method)
            && method.GetAttributes().Any(static attr => attr.AttributeClass?.Name == "HandlesResponseDisposalAttribute"));
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
        {string.Join(NewLine + "        ", desc.Parameters.Select(EmitAdd))}
        return new global::System.Net.Http.FormUrlEncodedContent(data);
    }}";

        static string EmitAdd(ParameterDescriptor p)
            => p.IsValueType
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
            {(desc.Parameters.Count == 0 ? string.Empty : "+ ")}{string.Join(NewLine + "            + ", desc.Parameters.Select((e, i) => $"$\"{(i == 0 ? '?' : '&')}{e.Key}={{Escape(StringifyArgument({e.Name}))}}\""))};
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
            ProtoOutputType.Json => @$"protected virtual {desc.ReturnType} {GetReadResponseMethodName(desc)}(global::System.Net.Http.HttpResponseMessage response, global::System.Threading.CancellationToken cancellationToken)
    {{
        return {(desc.AsyncReturnType == AsyncReturnType.ValueTask ? $"new {desc.ReturnType}(" : string.Empty)}global::System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync(response.Content, {Info.JsonSerializerContextType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Default.{desc.ReturnValueType.JsonContextName}, cancellationToken)!{(desc.AsyncReturnType == AsyncReturnType.ValueTask ? $")" : string.Empty)};
    }}",
            ProtoOutputType.Custom => string.Empty,
            var output => throw new InvalidOperationException($"Unsupported ouput type {output}.")
        };

    private string EmitMethod(MethodDescriptor desc, INamedTypeSymbol? clientType)
    {
        var requestVar = "request";
        var requestVarSeed = 0;
        while (desc.Parameters.Any(p => p.Name == requestVar))
        {
            requestVar = $"request{++requestVarSeed}";
        }
        var ctoken = desc.UsesCancellation ? "cancellationToken" : "global::System.Threading.CancellationToken.None";
        var errorHandlerName = $"Handle{desc.MethodId}Errors";
        var errorHandler = clientType is null
            ? "HandleErrors"
            : clientType.GetMembers().Any(m => m is IMethodSymbol meth && meth.Name == errorHandlerName)
                ? errorHandlerName
                : "HandleErrors";
        return @$"public virtual async {desc.ReturnType} {desc.MethodName}({string.Join(", ", desc.Parameters.Select(e => $"{e.TypeName} {e.Name}"))}{(desc.UsesCancellation ? (desc.Parameters.Count > 0 ? ", " : string.Empty) + "global::System.Threading.CancellationToken cancellationToken" : string.Empty)})
    {{
        var {requestVar} = Create{desc.MethodId}Request({string.Join(", ", desc.Parameters.Select(e => e.Name))});
        using var client = CreateHttpClient();
        {(ShouldDisposeResponse(desc) ? "using " : string.Empty)}var response = await client.SendAsync({requestVar}, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, {ctoken});
        await {errorHandler}(response, {ctoken});
        {(desc.NoReturn ? string.Empty : $"return await Read{desc.MethodId}Response(response, {ctoken});")}
    }}";
    }

    private string GetClientAccessiibility() => Info.ClientType.DeclaredAccessibility switch
    {
        Accessibility.NotApplicable => string.Empty,
        Accessibility.Private => "private ",
        Accessibility.ProtectedAndInternal => "private protected ",
        Accessibility.Protected => "protected ",
        Accessibility.Internal => "internal ",
        Accessibility.ProtectedOrInternal => "protected internal ",
        Accessibility.Public => "public ",
        _ => string.Empty
    };

    public string EmitClient(string @namespace, string name)
    {
        var accessibility = GetClientAccessiibility();
        return @$"#nullable enable
namespace {@namespace}
{{
{accessibility}sealed class {name}Configuration : global::NCoreUtils.Proto.IEndpointConfiguration
{{
    public string? HttpClient {{ get; set; }}

    public string Endpoint {{ get; set; }} = string.Empty;

    public string? Path {{ get; set; }}
}}

{accessibility}static class ServiceCollection{name}Extensions
{{
    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{name}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, {name}Configuration configuration)
        => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<{Info.InterfaceFullName}, {name}>(services, serviceProvider => new {name}(
            configuration: configuration{EmitHttpClientFactoryParameterInitializer(Info.NoHttpClientFactory)}{EmitAdditionalConstructorParametersInitialization(Info.AdditionalConstructorParameters)}
        ));

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
            Endpoint = configuration[""Endpoint""] ?? throw new global::System.InvalidOperationException(""Missing endpoint for {name}.""),
            Path = configuration[""Path""],
        }});
}}

{accessibility}partial class {name} : {(Info.NoHttpClientFactory ? "global::NCoreUtils.Proto.ProtoClientCore" : "global::NCoreUtils.Proto.ProtoClientBase")}, {Info.InterfaceFullName}
{{
    public enum Methods {{ {string.Join(", ", Info.Service.Methods.Select(e => e.MethodId))} }}

    private global::System.Collections.Concurrent.ConcurrentDictionary<Methods, string> MethodPathCache {{ get; }} = new global::System.Collections.Concurrent.ConcurrentDictionary<Methods, string>();

    private global::System.Func<Methods, string> MethodPathFactory {{ get; }}

    protected global::System.Collections.Generic.IReadOnlyDictionary<Methods, string> MethodPaths {{ get; }}

    protected string ServicePath {{ get; }} = ""{Info.Service.Path}"";

    protected override string HttpClientConfiguration => ""{Info.HttpClientConfiguration}"";

    {EmitAdditionalConstructorParameterProperties(Info.AdditionalConstructorParameters)}

    public {name}({name}Configuration configuration{(Info.NoHttpClientFactory ? string.Empty : ", global::System.Net.Http.IHttpClientFactory httpClientFactory")}{EmitAdditionalConstructorArguments(Info.AdditionalConstructorParameters)})
        : base(configuration{(Info.NoHttpClientFactory ? string.Empty : ", httpClientFactory")})
    {{
        if (!(configuration.Path is null))
        {{
            ServicePath = configuration.Path.Trim('/');
        }}
        var methodPaths = new global::System.Collections.Generic.Dictionary<Methods, string>({Info.Service.Methods.Count})
        {{
            {string.Join("," + NewLine + "            ", Info.Service.Methods.Select(e => $"{{ Methods.{e.MethodId}, \"{e.Path}\" }}"))}
        }};
        MethodPaths = methodPaths;
        MethodPathFactory = GetMethodPath;
        {EmitAdditionalConstructorParameterPropertyAssignments(Info.AdditionalConstructorParameters)}
    }}

    private string GetCachedMethodPath(Methods method)
        => MethodPathCache.GetOrAdd(method, MethodPathFactory);

    protected virtual string GetMethodPath(Methods method)
        => string.IsNullOrEmpty(ServicePath) ? MethodPaths[method] : $""{{ServicePath}}/{{MethodPaths[method]}}"";

    {string.Join(NewLine + "    ", Info.Service.Methods.Where(e => e.Input == ProtoInputType.Json).Select(EmitCreateJsonContentMethod))}

    {string.Join(NewLine + "    ", Info.Service.Methods.Where(e => e.Input == ProtoInputType.Form).Select(EmitCreateFormContentMethod))}

    {string.Join(NewLine + "    ", Info.Service.Methods.Select(EmitCreateRequestMethod))}

    {string.Join(NewLine + "    ", Info.Service.Methods.Select(EmitReadResponseMethod))}

    {string.Join(NewLine + "    ", Info.Service.Methods.Select(desc => EmitMethod(desc, Info.ClientType)))}
}}
}}";

        static string EmitAdditionalConstructorParameterProperty(ProtoClientConstructorParameter parameter)
            => $"protected {parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {parameter.Name} {{ get; }}";

        static string EmitAdditionalConstructorParameterProperties(IReadOnlyList<ProtoClientConstructorParameter> parameters)
        {
            return string.Join(NewLine + "    ", parameters.Select(EmitAdditionalConstructorParameterProperty));
        }

        static string EmitAdditionalConstructorParameterPropertyAssignment(ProtoClientConstructorParameter parameter)
            => $"this.{parameter.Name} = {parameter.Name};";

        static string EmitAdditionalConstructorParameterPropertyAssignments(IReadOnlyList<ProtoClientConstructorParameter> parameters)
        {
            return string.Join(NewLine + "        ", parameters.Select(EmitAdditionalConstructorParameterPropertyAssignment));
        }

        static string EmitAdditionalConstructorArguments(IReadOnlyList<ProtoClientConstructorParameter> parameters)
        {
            if (parameters.Count == 0)
            {
                return string.Empty;
            }
            return $", {string.Join(", ", parameters.Select(p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}"))}";
        }

        static string EmitAdditionalConstructorParameterInitialization(ProtoClientConstructorParameter parameter)
            => $"{parameter.Name}: global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(serviceProvider)";

        static string EmitAdditionalConstructorParametersInitialization(IReadOnlyList<ProtoClientConstructorParameter> parameters)
        {
            if (parameters.Count == 0)
            {
                return string.Empty;
            }
            return $",{NewLine}            {string.Join($",{NewLine}            ", parameters.Select(EmitAdditionalConstructorParameterInitialization))}";
        }

        static string EmitHttpClientFactoryParameterInitializer(bool noHttpClientFactory)
        {
            if (noHttpClientFactory)
            {
                return string.Empty;
            }
            return @",
            httpClientFactory: global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::System.Net.Http.IHttpClientFactory>(serviceProvider)";
        }
    }
}