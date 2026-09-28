using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Proto;

internal class ProtoClientEmitter(ProtoClientInfo info)
{
    private ProtoClientInfo Info { get; } = info;

    private static TypeSyntax Type(string name) => ParseTypeName(name);

    private static ExpressionSyntax Name(string name) => ParseName(name);

    private static ExpressionSyntax Id(string name) => IdentifierName(name);

    private static ExpressionSyntax Member(ExpressionSyntax target, string name)
        => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, IdentifierName(name));

    private static ExpressionSyntax GenericMember(ExpressionSyntax target, string name, params string[] types)
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            target,
            GenericName(Identifier(name)).WithTypeArgumentList(
                TypeArgumentList(SeparatedList(types.Select(type => Type(type))))));

    private static ArgumentListSyntax Arguments(params ExpressionSyntax[] values)
        => ArgumentList(SeparatedList(values.Select(value => Argument(value))));

    private static ArgumentListSyntax Arguments(IEnumerable<ArgumentSyntax> values)
        => ArgumentList(SeparatedList(values));

    private static ArgumentSyntax NamedArgument(string name, ExpressionSyntax value)
        => Argument(value).WithNameColon(NameColon(IdentifierName(name)));

    private static ExpressionSyntax Call(ExpressionSyntax target, params ExpressionSyntax[] arguments)
        => InvocationExpression(target, Arguments(arguments));

    private static ExpressionSyntax Call(ExpressionSyntax target, ArgumentListSyntax arguments)
        => InvocationExpression(target, arguments);

    private static ExpressionSyntax Create(string type, params ExpressionSyntax[] arguments)
        => ObjectCreationExpression(Type(type)).WithArgumentList(Arguments(arguments));

    private static ExpressionSyntax String(string value)
        => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));

    private static ExpressionSyntax Number(int value)
        => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value));

    private static ExpressionSyntax Null => LiteralExpression(SyntaxKind.NullLiteralExpression);

    private static ExpressionSyntax Default => LiteralExpression(SyntaxKind.DefaultLiteralExpression);

    private static ExpressionSyntax Assign(ExpressionSyntax left, ExpressionSyntax right)
        => AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right);

    private static StatementSyntax Statement(ExpressionSyntax expression) => ExpressionStatement(expression);

    private static StatementSyntax Local(string name, ExpressionSyntax value, bool usingDeclaration = false)
    {
        var statement = LocalDeclarationStatement(
            VariableDeclaration(IdentifierName("var")).WithVariables(SingletonSeparatedList(
                VariableDeclarator(Identifier(name)).WithInitializer(EqualsValueClause(value)))));
        return usingDeclaration ? statement.WithUsingKeyword(Token(SyntaxKind.UsingKeyword)) : statement;
    }

    private static ParameterSyntax Parameter(string type, string name)
        => SyntaxFactory.Parameter(Identifier(name)).WithType(Type(type));

    private static ParameterListSyntax Parameters(IEnumerable<ParameterSyntax> parameters)
        => ParameterList(SeparatedList(parameters));

    private static IEnumerable<ParameterSyntax> MethodParameters(MethodDescriptor desc)
        => desc.Parameters.Select(parameter => Parameter(parameter.TypeName, parameter.Name));

    private static ExpressionSyntax[] ParameterValues(MethodDescriptor desc)
        => desc.Parameters.Select(parameter => Id(parameter.Name)).ToArray();

    private static MethodDeclarationSyntax Method(string returnType, string name, IEnumerable<ParameterSyntax> parameters,
        IEnumerable<StatementSyntax> statements, params SyntaxKind[] modifiers)
        => MethodDeclaration(Type(returnType), name)
            .WithModifiers(TokenList(modifiers.Select(Token)))
            .WithParameterList(Parameters(parameters))
            .WithBody(Block(statements));

    private static MethodDeclarationSyntax ArrowMethod(string returnType, string name, IEnumerable<ParameterSyntax> parameters,
        ExpressionSyntax expression, params SyntaxKind[] modifiers)
        => MethodDeclaration(Type(returnType), name)
            .WithModifiers(TokenList(modifiers.Select(Token)))
            .WithParameterList(Parameters(parameters))
            .WithExpressionBody(ArrowExpressionClause(expression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private static PropertyDeclarationSyntax Property(
        string type,
        string name,
        SyntaxKind[] modifiers,
        bool setter = false,
        ExpressionSyntax? initializer = null)
    {
        var accessors = new List<AccessorDeclarationSyntax>
        {
            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
        };
        if (setter)
        {
            accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
        }
        var property = PropertyDeclaration(Type(type), name)
            .WithModifiers(TokenList(modifiers.Select(Token)))
            .WithAccessorList(AccessorList(List(accessors)));
        return initializer is null ? property : property.WithInitializer(EqualsValueClause(initializer))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static PropertyDeclarationSyntax ArrowProperty(string type, string name, ExpressionSyntax expression, params SyntaxKind[] modifiers)
        => PropertyDeclaration(Type(type), name)
            .WithModifiers(TokenList(modifiers.Select(Token)))
            .WithExpressionBody(ArrowExpressionClause(expression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private static InitializerExpressionSyntax ObjectInitializer(params (string Name, ExpressionSyntax Value)[] values)
        => InitializerExpression(SyntaxKind.ObjectInitializerExpression,
            SeparatedList<ExpressionSyntax>(values.Select(value => Assign(Id(value.Name), value.Value))));

    private static ExpressionSyntax NewWithInitializer(string type, params (string Name, ExpressionSyntax Value)[] values)
        => ObjectCreationExpression(Type(type)).WithArgumentList(Arguments())
            .WithInitializer(ObjectInitializer(values));

    private static InterpolatedStringTextSyntax InterpolatedText(string value)
        => InterpolatedStringText(Token(default, SyntaxKind.InterpolatedStringTextToken, value, value, default));

    private static InterpolatedStringExpressionSyntax Interpolated(params InterpolatedStringContentSyntax[] contents)
        => InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
            .WithContents(List(contents));

    private static string ReadResponseName(MethodDescriptor desc) => "Read" + desc.MethodId + "Response";

    private bool ShouldDisposeResponse(MethodDescriptor desc)
    {
        var methodName = ReadResponseName(desc);
        return !(Info.ClientType.GetMembers()
            .TryChooseFirst(methodName, static (mem, name) => mem is IMethodSymbol method && method.Name == name
                ? method.Choose() : default, out var method)
            && method.GetAttributes().Any(static attr => attr.AttributeClass?.Name == "HandlesResponseDisposalAttribute"));
    }

    private MethodDeclarationSyntax EmitJsonContent(MethodDescriptor desc)
    {
        var inputType = desc.InputDtoTypeName!;
        var context = Member(Member(Name(Info.JsonSerializerContextType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            "Default"), inputType.JsonContextName);
        if (inputType.IsNullableReference && desc.SingleJsonParameterWrapping == ProtoSingleJsonParameterWrapping.DoNotWrap
            && desc.Parameters.Count == 1)
        {
            context = PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, context);
        }
        var unwrapped = desc.SingleJsonParameterWrapping == ProtoSingleJsonParameterWrapping.DoNotWrap
            && desc.Parameters.Count == 1;
        var payload = unwrapped
            ? Id(desc.Parameters[0].Name)
            : Create(inputType.FullName, ParameterValues(desc));
        var result = Call(Member(Name("global::NCoreUtils.Proto.Internal.ProtoJsonContent"), "Create"),
            payload, context, Id("JsonMediaType"));
        return Method("global::System.Net.Http.HttpContent", "Create" + desc.MethodId + "RequestContent",
            MethodParameters(desc), [ReturnStatement(result)], SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
    }

    private MethodDeclarationSyntax EmitFormContent(MethodDescriptor desc)
    {
        var statements = new List<StatementSyntax>
        {
            Local("data", Create("global::System.Collections.Generic.Dictionary<string, string>", Number(desc.Parameters.Count)))
        };
        foreach (var parameter in desc.Parameters)
        {
            ExpressionSyntax condition = parameter.IsValueType
                ? BinaryExpression(SyntaxKind.NotEqualsExpression, Default, Id(parameter.Name))
                : IsPatternExpression(Id(parameter.Name),
                    UnaryPattern(Token(SyntaxKind.NotKeyword), ConstantPattern(Null)));
            var add = Call(Member(Id("data"), "Add"),
                String(parameter.Key), Call(Id("StringifyArgument"), Id(parameter.Name)));
            statements.Add(IfStatement(condition, Block(Statement(add))));
        }
        statements.Add(ReturnStatement(Create("global::System.Net.Http.FormUrlEncodedContent", Id("data"))));
        return Method("global::System.Net.Http.HttpContent", "Create" + desc.MethodId + "RequestContent",
            MethodParameters(desc), statements, SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
    }

    private MethodDeclarationSyntax? EmitRequest(MethodDescriptor desc)
    {
        if (desc.Input == ProtoInputType.Custom)
        {
            return null;
        }
        var methodName = "Create" + desc.MethodId + "Request";
        var cachedPath = Call(Id("GetCachedMethodPath"), Member(Id("Methods"), desc.MethodId));
        var verb = Member(Name("global::System.Net.Http.HttpMethod"), desc.Verb);
        if (desc.Input == ProtoInputType.Query)
        {
            ExpressionSyntax path = cachedPath;
            foreach (var (parameter, index) in desc.Parameters.Select((parameter, index) => (parameter, index)))
            {
                var escaped = Call(Id("Escape"), Call(Id("StringifyArgument"), Id(parameter.Name)));
                var suffix = Interpolated(InterpolatedText((index == 0 ? "?" : "&") + parameter.Key + "="),
                    Interpolation(escaped));
                path = BinaryExpression(SyntaxKind.AddExpression, path, suffix);
            }
            var statements = new List<StatementSyntax>
            {
                Local("path", path),
                ReturnStatement(Create("global::System.Net.Http.HttpRequestMessage", verb, Id("path")))
            };
            if (desc.Parameters.Count > 0)
            {
                var escape = LocalFunctionStatement(Type("string"), "Escape")
                    .AddModifiers(Token(SyntaxKind.StaticKeyword))
                    .WithParameterList(Parameters([Parameter("string?", "value")]))
                    .WithExpressionBody(ArrowExpressionClause(Call(Member(Name("global::System.Uri"), "EscapeDataString"),
                        BinaryExpression(SyntaxKind.CoalesceExpression, Id("value"), Member(Name("global::System.String"), "Empty")))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                statements.Add(escape);
            }
            return Method("global::System.Net.Http.HttpRequestMessage", methodName, MethodParameters(desc),
                statements, SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
        }

        var request = ObjectCreationExpression(Type("global::System.Net.Http.HttpRequestMessage"))
            .WithArgumentList(Arguments(verb, cachedPath))
            .WithInitializer(ObjectInitializer((
                "Content", Call(Id("Create" + desc.MethodId + "RequestContent"), ParameterValues(desc)))));
        return Method("global::System.Net.Http.HttpRequestMessage", methodName, MethodParameters(desc),
            [ReturnStatement(request)], SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
    }

    private MethodDeclarationSyntax? EmitReadResponse(MethodDescriptor desc)
    {
        if (desc.NoReturn || desc.Output == ProtoOutputType.Custom)
        {
            return null;
        }
        if (desc.Output != ProtoOutputType.Json)
        {
            throw new InvalidOperationException($"Unsupported ouput type {desc.Output}.");
        }
        var context = Member(Member(Name(Info.JsonSerializerContextType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            "Default"), desc.ReturnValueType.JsonContextName);
        ExpressionSyntax result = PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
            Call(Member(Name("global::System.Net.Http.Json.HttpContentJsonExtensions"), "ReadFromJsonAsync"),
                Member(Id("response"), "Content"), context, Id("cancellationToken")));
        if (desc.AsyncReturnType == AsyncReturnType.ValueTask)
        {
            result = Create(desc.ReturnType, result);
        }
        return Method(desc.ReturnType, ReadResponseName(desc),
        [
            Parameter("global::System.Net.Http.HttpResponseMessage", "response"),
            Parameter("global::System.Threading.CancellationToken", "cancellationToken")
        ], [ReturnStatement(result)], SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
    }

    private MethodDeclarationSyntax EmitMethod(MethodDescriptor desc)
    {
        var requestVar = "request";
        var requestVarSeed = 0;
        while (desc.Parameters.Any(parameter => parameter.Name == requestVar))
        {
            requestVar = "request" + ++requestVarSeed;
        }
        var ctoken = desc.UsesCancellation ? Id("cancellationToken")
            : Member(Name("global::System.Threading.CancellationToken"), "None");
        var errorHandlerName = "Handle" + desc.MethodId + "Errors";
        var errorHandler = Info.ClientType.GetMembers().Any(member =>
            member is IMethodSymbol method && method.Name == errorHandlerName) ? errorHandlerName : "HandleErrors";
        var parameters = MethodParameters(desc).ToList();
        if (desc.UsesCancellation)
        {
            parameters.Add(Parameter("global::System.Threading.CancellationToken", "cancellationToken"));
        }
        var statements = new List<StatementSyntax>
        {
            Local(requestVar, Call(Id("Create" + desc.MethodId + "Request"), ParameterValues(desc))),
            Local("client", Call(Id("CreateHttpClient")), usingDeclaration: true),
            Local("response", AwaitExpression(Call(Member(Id("client"), "SendAsync"),
                Id(requestVar), Member(Name("global::System.Net.Http.HttpCompletionOption"), "ResponseHeadersRead"), ctoken)),
                usingDeclaration: ShouldDisposeResponse(desc)),
            Statement(AwaitExpression(Call(Id(errorHandler), Id("response"), ctoken)))
        };
        if (!desc.NoReturn)
        {
            statements.Add(ReturnStatement(AwaitExpression(Call(Id(ReadResponseName(desc)), Id("response"), ctoken))));
        }
        return Method(desc.ReturnType, desc.MethodName, parameters, statements,
            SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword, SyntaxKind.AsyncKeyword);
    }

    private SyntaxTokenList Accessibility()
        => Info.ClientType.DeclaredAccessibility switch
        {
            Microsoft.CodeAnalysis.Accessibility.Private => TokenList(Token(SyntaxKind.PrivateKeyword)),
            Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ProtectedKeyword)),
            Microsoft.CodeAnalysis.Accessibility.Protected => TokenList(Token(SyntaxKind.ProtectedKeyword)),
            Microsoft.CodeAnalysis.Accessibility.Internal => TokenList(Token(SyntaxKind.InternalKeyword)),
            Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword)),
            Microsoft.CodeAnalysis.Accessibility.Public => TokenList(Token(SyntaxKind.PublicKeyword)),
            _ => TokenList()
        };

    private ClassDeclarationSyntax EmitConfiguration(string name)
    {
        var members = new MemberDeclarationSyntax[]
        {
            Property("string?", "HttpClient", [SyntaxKind.PublicKeyword], setter: true),
            Property("string", "Endpoint", [SyntaxKind.PublicKeyword], setter: true, initializer: Member(Name("global::System.String"), "Empty")),
            Property("string?", "Path", [SyntaxKind.PublicKeyword], setter: true)
        };
        return ClassDeclaration(name + "Configuration")
            .WithModifiers(Accessibility().Add(Token(SyntaxKind.SealedKeyword)))
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(Type("global::NCoreUtils.Proto.IEndpointConfiguration")))))
            .WithMembers(List(members));
    }

    private static ParameterSyntax ExtensionServicesParameter()
        => Parameter("global::Microsoft.Extensions.DependencyInjection.IServiceCollection", "services")
            .AddModifiers(Token(SyntaxKind.ThisKeyword));

    private ClassDeclarationSyntax EmitExtensions(string name)
    {
        var returnType = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
        var configurationType = name + "Configuration";
        var registrationArguments = new List<ArgumentSyntax>
        {
            NamedArgument("configuration", Id("configuration"))
        };
        if (!Info.NoHttpClientFactory)
        {
            registrationArguments.Add(NamedArgument("httpClientFactory", Call(
                GenericMember(Name("global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions"),
                    "GetRequiredService", "global::System.Net.Http.IHttpClientFactory"), Id("serviceProvider"))));
        }
        foreach (var parameter in Info.AdditionalConstructorParameters)
        {
            registrationArguments.Add(NamedArgument(parameter.Name, Call(
                GenericMember(Name("global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions"),
                    "GetRequiredService", parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                Id("serviceProvider"))));
        }
        var factory = SimpleLambdaExpression(SyntaxFactory.Parameter(Identifier("serviceProvider")),
            ObjectCreationExpression(Type(name)).WithArgumentList(Arguments(registrationArguments)));
        var singleton = Call(GenericMember(Name("Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions"),
            "AddSingleton", Info.InterfaceFullName, name),
            Id("services"), factory);
        var methods = new List<MemberDeclarationSyntax>
        {
            ArrowMethod(returnType, "Add" + name,
            [
                ExtensionServicesParameter(),
                Parameter(configurationType, "configuration")
            ], singleton, SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword)
        };

        var endpointConfig = NewWithInitializer(configurationType,
            ("HttpClient", Member(Id("configuration"), "HttpClient")),
            ("Endpoint", Member(Id("configuration"), "Endpoint")),
            ("Path", Id("path")));
        methods.Add(ArrowMethod(returnType, "Add" + name,
        [
            ExtensionServicesParameter(),
            Parameter("global::NCoreUtils.Proto.IEndpointConfiguration", "configuration"),
            Parameter("string?", "path").WithDefault(EqualsValueClause(Default))
        ], Call(Member(Id("services"), "Add" + name), endpointConfig),
            SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword));

        var stringConfig = NewWithInitializer(configurationType,
            ("HttpClient", Id("httpClientConfiguration")),
            ("Endpoint", Id("endpoint")),
            ("Path", Id("path")));
        methods.Add(ArrowMethod(returnType, "Add" + name,
        [
            ExtensionServicesParameter(),
            Parameter("string", "endpoint"),
            Parameter("string?", "httpClientConfiguration").WithDefault(EqualsValueClause(Default)),
            Parameter("string?", "path").WithDefault(EqualsValueClause(Default))
        ], Call(Member(Id("services"), "Add" + name), stringConfig),
            SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword));

        ExpressionSyntax ConfigValue(string key) => ElementAccessExpression(Id("configuration"))
            .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(Argument(String(key)))));
        var configured = NewWithInitializer(configurationType,
            ("HttpClient", ConfigValue("HttpClient")),
            ("Endpoint", BinaryExpression(SyntaxKind.CoalesceExpression, ConfigValue("Endpoint"),
                ThrowExpression(Create("global::System.InvalidOperationException",
                    String("Missing endpoint for " + name + "."))))),
            ("Path", ConfigValue("Path")));
        methods.Add(ArrowMethod(returnType, "Add" + name,
        [
            ExtensionServicesParameter(),
            Parameter("global::Microsoft.Extensions.Configuration.IConfiguration", "configuration")
        ], Call(Member(Id("services"), "Add" + name), configured),
            SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword));

        return ClassDeclaration("ServiceCollection" + name + "Extensions")
            .WithModifiers(Accessibility().Add(Token(SyntaxKind.StaticKeyword)))
            .WithMembers(List(methods));
    }

    private ConstructorDeclarationSyntax EmitConstructor(string name)
    {
        var parameters = new List<ParameterSyntax> { Parameter(name + "Configuration", "configuration") };
        var baseArguments = new List<ArgumentSyntax> { Argument(Id("configuration")) };
        if (!Info.NoHttpClientFactory)
        {
            parameters.Add(Parameter("global::System.Net.Http.IHttpClientFactory", "httpClientFactory"));
            baseArguments.Add(Argument(Id("httpClientFactory")));
        }
        parameters.AddRange(Info.AdditionalConstructorParameters.Select(parameter =>
            Parameter(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), parameter.Name)));
        var pathCondition = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
            ParenthesizedExpression(IsPatternExpression(Member(Id("configuration"), "Path"),
                ConstantPattern(Null))));
        var pathAssignment = Assign(Id("ServicePath"), Call(Member(Member(Id("configuration"), "Path"), "Trim"), LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal('/'))));
        var statements = new List<StatementSyntax>
        {
            IfStatement(pathCondition, Block(Statement(pathAssignment)))
        };
        var entries = Info.Service.Methods.Select(method => (ExpressionSyntax)InitializerExpression(
            SyntaxKind.ComplexElementInitializerExpression, SeparatedList(
            [
                Member(Id("Methods"), method.MethodId),
                String(method.Path)
            ])));
        var dictionary = ObjectCreationExpression(Type("global::System.Collections.Generic.Dictionary<Methods, string>"))
            .WithArgumentList(Arguments(Number(Info.Service.Methods.Count)))
            .WithInitializer(InitializerExpression(SyntaxKind.CollectionInitializerExpression,
                SeparatedList(entries)));
        statements.Add(Local("methodPaths", dictionary));
        statements.Add(Statement(Assign(Id("MethodPaths"), Id("methodPaths"))));
        statements.Add(Statement(Assign(Id("MethodPathFactory"), Id("GetMethodPath"))));
        statements.AddRange(Info.AdditionalConstructorParameters.Select(parameter =>
            Statement(Assign(Member(ThisExpression(), parameter.Name), Id(parameter.Name)))));
        return ConstructorDeclaration(name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(Parameters(parameters))
            .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, Arguments(baseArguments)))
            .WithBody(Block(statements));
    }

    private ClassDeclarationSyntax EmitClientClass(string name)
    {
        var methods = Info.Service.Methods;
        var enumMembers = methods.Select(method => EnumMemberDeclaration(method.MethodId));
        var members = new List<MemberDeclarationSyntax>
        {
            EnumDeclaration("Methods").AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithMembers(SeparatedList(enumMembers)),
            Property("global::System.Collections.Concurrent.ConcurrentDictionary<Methods, string>", "MethodPathCache",
                [SyntaxKind.PrivateKeyword], initializer:
                    Create("global::System.Collections.Concurrent.ConcurrentDictionary<Methods, string>")),
            Property("global::System.Func<Methods, string>", "MethodPathFactory", [SyntaxKind.PrivateKeyword]),
            Property("global::System.Collections.Generic.IReadOnlyDictionary<Methods, string>", "MethodPaths",
                [SyntaxKind.ProtectedKeyword]),
            Property("string", "ServicePath", [SyntaxKind.ProtectedKeyword], initializer: String(Info.Service.Path)),
            ArrowProperty("string", "HttpClientConfiguration", String(Info.HttpClientConfiguration),
                SyntaxKind.ProtectedKeyword, SyntaxKind.OverrideKeyword)
        };
        foreach (var parameter in Info.AdditionalConstructorParameters)
        {
            members.Add(Property(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), parameter.Name,
                [SyntaxKind.ProtectedKeyword]));
        }
        members.Add(EmitConstructor(name));
        members.Add(ArrowMethod("string", "GetCachedMethodPath", [Parameter("Methods", "method")],
            Call(Member(Id("MethodPathCache"), "GetOrAdd"), Id("method"), Id("MethodPathFactory")),
            SyntaxKind.PrivateKeyword));
        var methodPath = ElementAccessExpression(Id("MethodPaths")).WithArgumentList(
            BracketedArgumentList(SingletonSeparatedList(Argument(Id("method")))));
        var pathExpression = ConditionalExpression(
            Call(Member(Name("global::System.String"), "IsNullOrEmpty"), Id("ServicePath")),
            methodPath,
            Interpolated(Interpolation(Id("ServicePath")), InterpolatedText("/"), Interpolation(methodPath)));
        members.Add(ArrowMethod("string", "GetMethodPath", [Parameter("Methods", "method")],
            pathExpression, SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword));

        members.AddRange(methods.Where(method => method.Input == ProtoInputType.Json).Select(EmitJsonContent));
        members.AddRange(methods.Where(method => method.Input == ProtoInputType.Form).Select(EmitFormContent));
        members.AddRange(methods.Select(EmitRequest).Where(method => method is not null)!);
        members.AddRange(methods.Select(EmitReadResponse).Where(method => method is not null)!);
        members.AddRange(methods.Select(EmitMethod));

        return ClassDeclaration(name)
            .WithModifiers(Accessibility().Add(Token(SyntaxKind.PartialKeyword)))
            .WithBaseList(BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(Type(Info.NoHttpClientFactory
                    ? "global::NCoreUtils.Proto.ProtoClientCore" : "global::NCoreUtils.Proto.ProtoClientBase")),
                SimpleBaseType(Type(Info.InterfaceFullName))
            })))
            .WithMembers(List(members));
    }

    public CompilationUnitSyntax EmitClient(string @namespace, string name)
        => CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                NamespaceDeclaration(ParseName(@namespace))
                    .WithLeadingTrivia(ParseLeadingTrivia("#nullable enable\n"))
                    .WithMembers(List<MemberDeclarationSyntax>(
                    [
                        EmitConfiguration(name),
                        EmitExtensions(name),
                        EmitClientClass(name)
                    ]))));
}
