using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Proto;

internal class ProtoImplEmitter(ProtoImplInfo info, ProtoImplEmitterContext context)
{
    private ProtoImplInfo Info { get; } = info ?? throw new ArgumentNullException(nameof(info));

    private ProtoImplEmitterContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    private static TypeSyntax Type(string name) => ParseTypeName(name);

    private static ExpressionSyntax Name(string name) => ParseName(name);

    private static ExpressionSyntax Id(string name) => IdentifierName(name);

    private static ExpressionSyntax Member(ExpressionSyntax target, string name)
        => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, IdentifierName(name));

    private static ExpressionSyntax GenericMember(ExpressionSyntax target, string name, params string[] types)
        => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target,
            GenericName(Identifier(name)).WithTypeArgumentList(
                TypeArgumentList(SeparatedList(types.Select(type => Type(type))))));

    private static ArgumentListSyntax Arguments(params ExpressionSyntax[] values)
        => ArgumentList(SeparatedList(values.Select(value => Argument(value))));

    private static ExpressionSyntax Call(ExpressionSyntax target, params ExpressionSyntax[] values)
        => InvocationExpression(target, Arguments(values));

    private static ExpressionSyntax Call(ExpressionSyntax target, IEnumerable<ArgumentSyntax> values)
        => InvocationExpression(target, ArgumentList(SeparatedList(values)));

    private static ExpressionSyntax Create(string type, params ExpressionSyntax[] values)
        => ObjectCreationExpression(Type(type)).WithArgumentList(Arguments(values));

    private static ExpressionSyntax String(string value)
        => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));

    private static ExpressionSyntax Number(int value)
        => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value));

    private static ExpressionSyntax Null => LiteralExpression(SyntaxKind.NullLiteralExpression);

    private static ExpressionSyntax Assign(ExpressionSyntax left, ExpressionSyntax right)
        => AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right);

    private static StatementSyntax Statement(ExpressionSyntax expression) => ExpressionStatement(expression);

    private static StatementSyntax Local(string name, ExpressionSyntax value, string type = "var")
        => LocalDeclarationStatement(
            VariableDeclaration(Type(type)).WithVariables(SingletonSeparatedList(
                VariableDeclarator(Identifier(name)).WithInitializer(EqualsValueClause(value)))));

    private static ParameterSyntax Parameter(string type, string name)
        => SyntaxFactory.Parameter(Identifier(name)).WithType(Type(type));

    private static ParameterListSyntax Parameters(params ParameterSyntax[] values)
        => ParameterList(SeparatedList(values));

    private static MethodDeclarationSyntax Method(string returnType, string name,
        IEnumerable<ParameterSyntax> parameters, IEnumerable<StatementSyntax> statements, params SyntaxKind[] modifiers)
        => MethodDeclaration(Type(returnType), name)
            .WithModifiers(TokenList(modifiers.Select(Token)))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithBody(Block(statements));

    private static MethodDeclarationSyntax ArrowMethod(string returnType, string name,
        IEnumerable<ParameterSyntax> parameters, ExpressionSyntax expression, params SyntaxKind[] modifiers)
        => MethodDeclaration(Type(returnType), name)
            .WithModifiers(TokenList(modifiers.Select(Token)))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithExpressionBody(ArrowExpressionClause(expression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private static PropertyDeclarationSyntax AutoProperty(string type, string name,
        SyntaxTokenList modifiers, ExpressionSyntax? initializer = null)
    {
        var property = PropertyDeclaration(Type(type), name)
            .WithModifiers(modifiers)
            .WithAccessorList(AccessorList(SingletonList(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))));
        return initializer is null ? property : property.WithInitializer(EqualsValueClause(initializer))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static ExpressionSyntax Index(ExpressionSyntax target, ExpressionSyntax index)
        => ElementAccessExpression(target).WithArgumentList(
            BracketedArgumentList(SingletonSeparatedList(Argument(index))));

    private static ExpressionSyntax Lambda(string parameter, ExpressionSyntax expression)
        => SimpleLambdaExpression(SyntaxFactory.Parameter(Identifier(parameter)), expression);

    private static ExpressionSyntax Lambda(string parameter, IEnumerable<StatementSyntax> statements)
        => SimpleLambdaExpression(SyntaxFactory.Parameter(Identifier(parameter)), Block(statements));

    private static ExpressionSyntax IsNull(ExpressionSyntax expression)
        => IsPatternExpression(expression, ConstantPattern(Null));

    private static string ListOf(string type)
        => "global::System.Collections.Generic.List<" + type + ">";

    private static string ActionOf(string type)
        => "global::System.Action<" + type + ">";

    private static string DictionaryOf(string key, string value)
        => "global::System.Collections.Generic.Dictionary<" + key + ", " + value + ">";

    private static string ReadName(MethodDescriptor desc) => "Read" + desc.MethodId + "RequestAsync";

    private static string WriteErrorName(MethodDescriptor desc) => "Write" + desc.MethodId + "ErrorAsync";

    private static string WriteResultName(MethodDescriptor desc) => "Write" + desc.MethodId + "ResultAsync";

    private static ParameterSyntax[] ReaderParameters()
        =>
        [
            Parameter("global::Microsoft.AspNetCore.Http.HttpRequest", "request"),
            Parameter("global::System.Threading.CancellationToken", "cancellationToken")
        ];

    private ExpressionSyntax ReadFormArgument(ITypeSymbol? implType, ParameterDescriptor parameter)
    {
        var methodName = "ReadArgumentOf" + parameter.TypeInfoPropertyName;
        if (implType is not null && implType.GetMembers().OfType<IMethodSymbol>()
            .TryGetFirst(method => method.Parameters.Length == 1 && method.Name == methodName, out var custom))
        {
            return Call(Id(custom.Name), Index(Id("data"), String(parameter.Key)));
        }
        return Call(GenericName(Identifier("ReadArgument")).WithTypeArgumentList(
            TypeArgumentList(SingletonSeparatedList(Type(parameter.TypeName)))),
            Index(Id("data"), String(parameter.Key)));
    }

    private MethodDeclarationSyntax EmitFormReader(MethodDescriptor desc, ITypeSymbol? implType)
    {
        ExpressionSyntax result;
        if (desc.InputDtoTypeName?.IsWrapper == true || desc.Parameters.Count > 1)
        {
            result = Create(desc.InputDtoTypeName!.FullName,
                desc.Parameters.Select(parameter => ReadFormArgument(implType, parameter)).ToArray());
        }
        else
        {
            if (desc.Parameters.Count != 1)
            {
                throw new InvalidOperationException("Trying to emit reader for method without parameters...");
            }
            result = ReadFormArgument(implType, desc.Parameters[0]);
        }
        return Method("global::System.Threading.Tasks.Task<" + desc.InputDtoTypeName!.FullName + ">",
            ReadName(desc), ReaderParameters(),
            [
                Local("data", AwaitExpression(Call(Member(Id("request"), "ReadFormAsync"), Id("cancellationToken")))),
                ReturnStatement(result)
            ], SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword, SyntaxKind.AsyncKeyword);
    }

    private MethodDeclarationSyntax? EmitQueryReader(MethodDescriptor desc)
    {
        if (desc.Parameters.Count == 0)
        {
            return null;
        }
        ExpressionSyntax ReadQueryArgument(ParameterDescriptor parameter)
            => Call(GenericName(Identifier("ReadArgument")).WithTypeArgumentList(
                    TypeArgumentList(SingletonSeparatedList(Type(parameter.TypeName)))),
                Index(Id("data"), String(parameter.Key)));
        ExpressionSyntax value = desc.Parameters.Count == 1 && !desc.InputDtoTypeName!.IsWrapper
            ? ReadQueryArgument(desc.Parameters[0])
            : Create(desc.InputDtoTypeName!.FullName,
                [.. desc.Parameters.Select(ReadQueryArgument)]);
        return Method("global::System.Threading.Tasks.ValueTask<" + desc.InputDtoTypeName + ">",
            ReadName(desc), ReaderParameters(),
            [
                Local("data", Member(Id("request"), "Query")),
                ReturnStatement(Create("global::System.Threading.Tasks.ValueTask<" + desc.InputDtoTypeName + ">", value))
            ], SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
    }

    private MethodDeclarationSyntax? EmitRequestReader(MethodDescriptor desc, ITypeSymbol? implType)
    {
        if (desc.Input == ProtoInputType.Form)
        {
            return EmitFormReader(desc, implType);
        }
        if (desc.Input == ProtoInputType.Query)
        {
            return EmitQueryReader(desc);
        }
        if (desc.Input != ProtoInputType.Json)
        {
            return null;
        }
        var dto = desc.InputDtoTypeName!;
        var context = Member(Member(Name(Info.JsonSerializerContextType!.ToDisplayString()), "Default"),
            dto.JsonContextName);
        ExpressionSyntax result = AwaitExpression(Call(Member(Name("global::System.Text.Json.JsonSerializer"),
            "DeserializeAsync"), Member(Id("request"), "Body"), context, Id("cancellationToken")));
        if (!dto.IsNullableReference && !dto.IsValueType)
        {
            var message = "Unable to deserialize JSON arguments for " + Info.InterfaceFullName + "." + desc.MethodName + ".";
            result = BinaryExpression(SyntaxKind.CoalesceExpression,
                ParenthesizedExpression(result),
                ThrowExpression(Create("global::NCoreUtils.Proto.ProtoException",
                    String("generic_error"), String(message))));
        }
        return ArrowMethod("global::System.Threading.Tasks.ValueTask<" + dto + ">",
            ReadName(desc), ReaderParameters(), result,
            SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword, SyntaxKind.AsyncKeyword);
    }

    private MethodDeclarationSyntax EmitErrorWriter(MethodDescriptor desc)
        => ArrowMethod("global::System.Threading.Tasks.Task", WriteErrorName(desc),
        [
            Parameter("global::Microsoft.Extensions.Logging.ILogger", "logger"),
            Parameter("global::Microsoft.AspNetCore.Http.HttpResponse", "response"),
            Parameter("global::System.Exception", "exn"),
            Parameter("global::System.Threading.CancellationToken", "cancellationToken")
        ], Call(Id("WriteErrorAsync"), Id("logger"), Id("response"), Id("exn"), Id("cancellationToken")),
            SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);

    private MethodDeclarationSyntax? EmitResultWriter(MethodDescriptor desc)
    {
        if (desc.NoReturn || desc.Output != ProtoOutputType.Json)
        {
            return null;
        }
        var context = PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
            Member(Member(Name(Info.JsonSerializerContextType!.ToDisplayString()), "Default"),
                desc.ReturnValueType.JsonContextName));
        return Method("global::System.Threading.Tasks.Task", WriteResultName(desc),
        [
            Parameter("global::Microsoft.AspNetCore.Http.HttpResponse", "response"),
            Parameter(desc.ReturnValueType.FullName, "result"),
            Parameter("global::System.Threading.CancellationToken", "cancellationToken")
        ],
        [
            Statement(Assign(Member(Id("response"), "ContentType"), String("application/json; charset=utf-8"))),
            ReturnStatement(Call(Member(Name("global::System.Text.Json.JsonSerializer"), "SerializeAsync"),
                Member(Id("response"), "Body"), Id("result"), context, Id("cancellationToken")))
        ], SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
    }

    private ExpressionSyntax ShouldPassException(ITypeSymbol? implType)
    {
        if (implType is not null && implType.GetMembers().OfType<IMethodSymbol>()
            .TryGetFirst(method => method.Name == "ShouldPassException", out var custom))
        {
            var arguments = new List<ExpressionSyntax>();
            foreach (var parameter in custom.Parameters)
            {
                if (Context.IsCancellationToken(parameter.Type))
                {
                    arguments.Add(Member(Id("httpContext"), "RequestAborted"));
                }
                else if (Context.IsException(parameter.Type))
                {
                    arguments.Add(Id("exn"));
                }
                else if (Context.IsHttpContext(parameter.Type))
                {
                    arguments.Add(Id("httpContext"));
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot provider argument of type {parameter.Type} for {implType.Name}.{custom.Name}.");
                }
            }
            return Call(Id(custom.Name), arguments.ToArray());
        }
        return BinaryExpression(SyntaxKind.LogicalAndExpression,
            IsPatternExpression(Id("exn"), TypePattern(Type("global::System.OperationCanceledException"))),
            Member(Member(Id("httpContext"), "RequestAborted"), "IsCancellationRequested"));
    }

    private MethodDeclarationSyntax EmitMethodInvoker(MethodDescriptor desc, ITypeSymbol? implType)
    {
        var requestAborted = Member(Id("httpContext"), "RequestAborted");
        var tryStatements = new List<StatementSyntax>();
        if (desc.Parameters.Count > 0)
        {
            tryStatements.Add(Local("arguments", AwaitExpression(Call(Id(ReadName(desc)),
                Member(Id("httpContext"), "Request"), requestAborted))));
        }
        var callArguments = new List<ExpressionSyntax>();
        if (desc.SingleJsonParameterWrapping == ProtoSingleJsonParameterWrapping.DoNotWrap
            && desc.Parameters.Count == 1)
        {
            callArguments.Add(Id("arguments"));
        }
        else
        {
            callArguments.AddRange(desc.Parameters.Select(parameter => Member(Id("arguments"), parameter.Name)));
        }
        if (desc.UsesCancellation)
        {
            callArguments.Add(requestAborted);
        }
        var invoke = AwaitExpression(Call(Member(Id("Impl"), desc.MethodName), callArguments.ToArray()));
        if (desc.NoReturn)
        {
            tryStatements.Add(Statement(invoke));
        }
        else
        {
            tryStatements.Add(Local("result", invoke));
            tryStatements.Add(Statement(AwaitExpression(Call(Id(WriteResultName(desc)),
                Member(Id("httpContext"), "Response"), Id("result"), requestAborted))));
        }
        var loggerFactory = Call(GenericMember(
                Name("global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions"),
                "GetRequiredService", "global::Microsoft.Extensions.Logging.ILoggerFactory"),
            Member(Id("httpContext"), "RequestServices"));
        var logger = Call(GenericMember(
            Name("global::Microsoft.Extensions.Logging.LoggerFactoryExtensions"),
            "CreateLogger", Info.ImplType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            loggerFactory);
        var catchStatements = new StatementSyntax[]
        {
            IfStatement(ShouldPassException(implType), Block(ThrowStatement())),
            Local("logger", logger),
            Statement(AwaitExpression(Call(Id(WriteErrorName(desc)), Id("logger"),
                Member(Id("httpContext"), "Response"), Id("exn"), requestAborted)))
        };
        var catchClause = CatchClause()
            .WithDeclaration(CatchDeclaration(Type("global::System.Exception"), Identifier("exn")))
            .WithBlock(Block(catchStatements));
        return Method("global::System.Threading.Tasks.Task", "Invoke" + desc.MethodId + "Async",
            [Parameter("global::Microsoft.AspNetCore.Http.HttpContext", "httpContext")],
            [TryStatement(Block(tryStatements), SingletonList(catchClause), default)],
            SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword, SyntaxKind.VirtualKeyword,
            SyntaxKind.AsyncKeyword);
    }

    private SyntaxTokenList Accessibility()
        => Info.ServiceType.DeclaredAccessibility switch
        {
            Microsoft.CodeAnalysis.Accessibility.Private => TokenList(Token(SyntaxKind.PrivateKeyword)),
            Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ProtectedKeyword)),
            Microsoft.CodeAnalysis.Accessibility.Protected => TokenList(Token(SyntaxKind.ProtectedKeyword)),
            Microsoft.CodeAnalysis.Accessibility.Internal => TokenList(Token(SyntaxKind.InternalKeyword)),
            Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword)),
            Microsoft.CodeAnalysis.Accessibility.Public => TokenList(Token(SyntaxKind.PublicKeyword)),
            _ => TokenList()
        };

    private ClassDeclarationSyntax EmitRootClass(string rootName)
        => ClassDeclaration(rootName)
            .WithModifiers(Accessibility().Add(Token(SyntaxKind.PartialKeyword)))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                EnumDeclaration("Methods").AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithMembers(SeparatedList(Info.Service.Methods.Select(
                        method => EnumMemberDeclaration(method.MethodId))))));

    private ClassDeclarationSyntax EmitImplementation(string name, ITypeSymbol? implType)
    {
        var constructorValue = BinaryExpression(SyntaxKind.CoalesceExpression, Id("impl"),
            ThrowExpression(Create("global::System.ArgumentNullException",
                InvocationExpression(IdentifierName("nameof"), Arguments(Id("impl"))))));
        var constructor = ConstructorDeclaration(name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(Parameters(Parameter(Info.InterfaceFullName, "impl")))
            .WithExpressionBody(ArrowExpressionClause(Assign(Id("Impl"), constructorValue)))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        var members = new List<MemberDeclarationSyntax>
        {
            AutoProperty(Info.InterfaceFullName, "Impl", TokenList(Token(SyntaxKind.PublicKeyword))),
            constructor
        };
        members.AddRange(Info.Service.Methods.Select(method => EmitRequestReader(method, implType))
            .Where(method => method is not null)!);
        members.AddRange(Info.Service.Methods.Select(EmitErrorWriter));
        members.AddRange(Info.Service.Methods.Select(EmitResultWriter).Where(method => method is not null)!);
        members.AddRange(Info.Service.Methods.Select(method => EmitMethodInvoker(method, implType)));
        return ClassDeclaration(name)
            .WithModifiers(Accessibility().Add(Token(SyntaxKind.PartialKeyword)))
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(Type("global::NCoreUtils.AspNetCore.ProtoImplementationBase")))))
            .WithMembers(List(members));
    }

    private static MethodDeclarationSyntax EmitSegmentsCombine()
    {
        const string segmentType = "global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment";
        var factory = Name("global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory");
        var segment = Call(Member(factory, "Segment"),
            Call(Member(factory, "LiteralPart"), Id("rawSegment")));
        var pattern = Member(factory, "Pattern");
        return Method("global::Microsoft.AspNetCore.Routing.Patterns.RoutePattern", "Combine",
        [
            Parameter("global::System.Collections.Generic.IEnumerable<" + segmentType + ">", "parentSegments"),
            Parameter("string", "rawSegment")
        ],
        [
            Local("segment", segment),
            ReturnStatement(ConditionalExpression(
                IsNull(Id("parentSegments")),
                Call(pattern, Id("segment")),
                Call(pattern, Call(Member(Name("global::System.Linq.Enumerable"), "Append"),
                    Id("parentSegments"), Id("segment")))))
        ], SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword);
    }

    private static PropertyDeclarationSyntax EndpointsProperty()
    {
        var set = Statement(Assign(Id("_endpoints"), Call(Id("BuildEndpoints"))));
        var locked = LockStatement(Id("Sync"), Block(
            IfStatement(IsNull(Id("_endpoints")), Block(set))));
        var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithBody(Block(
            IfStatement(IsNull(Id("_endpoints")), Block(locked)),
            ReturnStatement(Id("_endpoints"))));
        return PropertyDeclaration(Type("global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.Endpoint>"),
                "Endpoints")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword))
            .WithAccessorList(AccessorList(SingletonList(getter)));
    }

    private ExpressionSyntax ServiceSegments()
    {
        const string segmentType = "global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment";
        var factory = Name("global::Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory");
        var segments = Info.Service.Path.Split('/')
            .Select(segment => segment.Trim())
            .Where(segment => !string.IsNullOrEmpty(segment))
            .Select(segment => Call(Member(factory, "Segment"),
                Call(Member(factory, "LiteralPart"), String(segment))));
        var arrayType = ArrayType(Type(segmentType)).WithRankSpecifiers(
            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression()))));
        var constant = ArrayCreationExpression(arrayType)
            .WithInitializer(InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                SeparatedList(segments)));
        var source = Call(Member(Id("Path"), "Split"),
            LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal('/')));
        var trim = Call(Member(Id("s"), "Trim"));
        var nonempty = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
            Call(Member(Name("global::System.String"), "IsNullOrEmpty"), Id("s")));
        var selected = Call(Member(Name("global::System.Linq.Enumerable"), "Select"), source,
            Lambda("s", trim));
        var filtered = Call(Member(Name("global::System.Linq.Enumerable"), "Where"), selected,
            Lambda("s", nonempty));
        var mapped = Call(Member(Name("global::System.Linq.Enumerable"), "Select"), filtered,
            Lambda("s", Call(Member(factory, "Segment"),
                Call(Member(factory, "LiteralPart"), Id("s")))));
        return ConditionalExpression(IsNull(Id("Path")), constant,
            Call(Member(Name("global::System.Linq.Enumerable"), "ToArray"), mapped));
    }

    private IEnumerable<StatementSyntax> EmitAddEndpoint(MethodDescriptor desc, string rootName, string name)
    {
        var suffix = desc.MethodId;
        var delegateName = "delegate" + suffix;
        var builderName = "builder" + suffix;
        var conventionsName = "conventions" + suffix;
        var provider = Member(Id("httpContext"), "RequestServices");
        var createImpl = Info.ImplementationFactory is null
            ? Call(GenericMember(Name("global::Microsoft.Extensions.DependencyInjection.ActivatorUtilities"),
                "CreateInstance", name), provider)
            : Call(Member(Name(Info.ImplementationFactory.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                "CreateService"), provider);
        var requestDelegate = Lambda("httpContext",
        [
            Local("impl", createImpl),
            ReturnStatement(Call(Member(Id("impl"), "Invoke" + suffix + "Async"), Id("httpContext")))
        ]);
        var builder = ObjectCreationExpression(Type("global::Microsoft.AspNetCore.Routing.RouteEndpointBuilder"))
            .WithArgumentList(Arguments(Id(delegateName),
                Call(Member(Id("Segments"), "Combine"), Id("servicePathSegments"), String(desc.Path)),
                Number(100)))
            .WithInitializer(InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                SingletonSeparatedList<ExpressionSyntax>(Assign(Id("DisplayName"),
                    String(Info.InterfaceType.Name + "." + suffix)))));
        var statements = new List<StatementSyntax>
        {
            Local(delegateName, requestDelegate, "global::Microsoft.AspNetCore.Http.RequestDelegate"),
            Local(builderName, builder),
            ForEachStatement(Type("var"), Identifier("convention"), Id("Conventions"),
                Block(Statement(Call(Id("convention"), Id(builderName)))))
        };
        if (desc.HttpMethod != ProtoHttpMethod.Default)
        {
            var verbArray = ImplicitArrayCreationExpression(
                InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                    SingletonSeparatedList<ExpressionSyntax>(Member(
                        Name("global::Microsoft.AspNetCore.Http.HttpMethods"), desc.HttpMethod.ToString()))));
            statements.Add(Statement(Call(Member(Member(Id(builderName), "Metadata"), "Add"),
                Create("global::Microsoft.AspNetCore.Routing.HttpMethodMetadata", verbArray))));
        }
        var method = Member(Member(Id(rootName), "Methods"), suffix);
        var outArgument = Argument(DeclarationExpression(IdentifierName("var"),
                SingleVariableDesignation(Identifier(conventionsName))))
            .WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
        var lookup = Call(Member(Id("MethodConventions"), "TryGetValue"),
            [Argument(method), outArgument]);
        statements.Add(IfStatement(lookup, Block(
            ForEachStatement(Type("var"), Identifier("convention"), Id(conventionsName),
                Block(Statement(Call(Id("convention"), Id(builderName))))))));
        statements.Add(Statement(Call(Member(Id("endpoints"), "Add"), Call(Member(Id(builderName), "Build")))));
        return statements;
    }

    private MethodDeclarationSyntax EmitBuildEndpoints(string rootName, string name)
    {
        var statements = new List<StatementSyntax>
        {
            Local("endpoints", Create(ListOf("global::Microsoft.AspNetCore.Http.Endpoint"),
                Number(Info.Service.Methods.Count))),
            Local("servicePathSegments", ServiceSegments())
        };
        foreach (var method in Info.Service.Methods)
        {
            statements.AddRange(EmitAddEndpoint(method, rootName, name));
        }
        statements.Add(ReturnStatement(Id("endpoints")));
        return Method("global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.Endpoint>",
            "BuildEndpoints", [], statements,
            SyntaxKind.PrivateKeyword);
    }

    private ClassDeclarationSyntax EmitDataSource(string rootName, string name)
    {
        var dataSourceName = name + "DataSource";
        const string endpointBuilder = "global::Microsoft.AspNetCore.Builder.EndpointBuilder";
        var action = ActionOf(endpointBuilder);
        var list = ListOf(action);
        var methods = rootName + ".Methods";
        var conventions = DictionaryOf(methods, list);
        var members = new List<MemberDeclarationSyntax>
        {
            ClassDeclaration("Segments")
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword))
                .WithMembers(SingletonList<MemberDeclarationSyntax>(EmitSegmentsCombine())),
            FieldDeclaration(VariableDeclaration(
                Type("global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.Endpoint>?"))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("_endpoints")))))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword)),
            AutoProperty(list, "Conventions", TokenList(Token(SyntaxKind.PrivateKeyword)),
                Create(list)),
            AutoProperty(conventions, "MethodConventions", TokenList(Token(SyntaxKind.PrivateKeyword)),
                Create(conventions)),
            AutoProperty("object", "Sync", TokenList(Token(SyntaxKind.PrivateKeyword)), Create("object")),
            AutoProperty("string?", "Path", TokenList(Token(SyntaxKind.PrivateKeyword))),
            EndpointsProperty(),
            ConstructorDeclaration(dataSourceName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithParameterList(Parameters(Parameter("string?", "path")))
                .WithExpressionBody(ArrowExpressionClause(Assign(Id("Path"), Id("path"))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            EmitBuildEndpoints(rootName, name),
            ArrowMethod("void", "Add", [Parameter(action, "convention")],
                Call(Member(Id("Conventions"), "Add"), Id("convention")),
                SyntaxKind.PublicKeyword),
            Method("void", "Add",
            [
                Parameter(methods, "method"), Parameter(action, "convention")
            ],
            [
                IfStatement(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                    Call(Member(Id("MethodConventions"), "TryGetValue"),
                        [
                            Argument(Id("method")),
                            Argument(DeclarationExpression(IdentifierName("var"),
                                SingleVariableDesignation(Identifier("conventions"))))
                                .WithRefKindKeyword(Token(SyntaxKind.OutKeyword))
                        ])),
                    Block(
                        Statement(Assign(Id("conventions"), Create(list))),
                        Statement(Call(Member(Id("MethodConventions"), "Add"),
                            Id("method"), Id("conventions"))))),
                Statement(Call(Member(Id("conventions"), "Add"), Id("convention")))
            ], SyntaxKind.PublicKeyword),
            ArrowMethod("global::Microsoft.Extensions.Primitives.IChangeToken", "GetChangeToken",
                [],
                Member(Name("global::Microsoft.Extensions.FileProviders.NullChangeToken"), "Singleton"),
                SyntaxKind.PublicKeyword, SyntaxKind.OverrideKeyword)
        };
        return ClassDeclaration(dataSourceName)
            .WithModifiers(Accessibility())
            .WithBaseList(BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(Type("global::Microsoft.AspNetCore.Routing.EndpointDataSource")),
                SimpleBaseType(Type("global::NCoreUtils.AspNetCore.IProtoEndpointsConventionBuilder<" + methods + ">"))
            })))
            .WithMembers(List(members));
    }

    private ClassDeclarationSyntax EmitExtensions(string rootName, string name)
    {
        var methodName = "Map" + Info.ImplType.Name;
        var dataSourceName = name + "DataSource";
        var map = Method("global::NCoreUtils.AspNetCore.IProtoEndpointsConventionBuilder<" + rootName + ".Methods>",
            methodName,
            [
                Parameter("global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder", "endpoints")
                    .AddModifiers(Token(SyntaxKind.ThisKeyword)),
                Parameter("string?", "path").WithDefault(EqualsValueClause(
                    LiteralExpression(SyntaxKind.DefaultLiteralExpression)))
            ],
            [
                Local("dataSource", Create(dataSourceName, Id("path"))),
                Statement(Call(Member(Member(Id("endpoints"), "DataSources"), "Add"), Id("dataSource"))),
                ReturnStatement(Id("dataSource"))
            ], SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword);
        return ClassDeclaration("EndpointRouteBuilder" + name + "Extensions")
            .WithModifiers(Accessibility().Add(Token(SyntaxKind.StaticKeyword)))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(map));
    }

    public CompilationUnitSyntax EmitImpl(string @namespace, string rootName, string name, ITypeSymbol? implType)
        => CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                NamespaceDeclaration(ParseName(@namespace))
                    .WithLeadingTrivia(ParseLeadingTrivia("#nullable enable\n"))
                    .WithMembers(List<MemberDeclarationSyntax>(
                    [
                        EmitRootClass(rootName),
                        EmitImplementation(name, implType),
                        EmitDataSource(rootName, name),
                        EmitExtensions(rootName, name)
                    ]))));
}
