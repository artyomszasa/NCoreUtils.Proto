using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Proto;

[Generator]
public class ProtoInfoGenerator : ISourceGenerator
{
    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Proto
{
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
public class ProtoInfoAttribute : System.Attribute
{
    public System.Type Target { get; }

    public InputType Input { get; set; }

    public OutputType Output { get; set; }

    public ErrorType Error { get; set; }

    public Naming Naming { get; set; }

    public Naming ParameterNaming { get; set; }

    public SingleJsonParameterWrapping SingleJsonParameterWrapping { get; set; }

    public bool KeepAsyncSuffix { get; set; }

    public string? Path { get; set; }

    public ProtoInfoAttribute(System.Type target)
        => Target = target ?? throw new System.ArgumentNullException(nameof(target));
}

[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public class ProtoMethodInfoAttribute : System.Attribute
{
    public string MethodName { get; }

    public InputType Input { get; set; }

    public OutputType Output { get; set; }

    public ErrorType Error { get; set; }

    public Naming Naming { get; set; }

    public Naming ParameterNaming { get; set; }

    public SingleJsonParameterWrapping SingleJsonParameterWrapping { get; set; }

    public bool KeepAsyncSuffix { get; set; }

    public string? Path { get; set; }

    public ProtoMethodInfoAttribute(string methodName)
        => MethodName = methodName ?? throw new System.ArgumentNullException(nameof(methodName));
}
}
";

    private static UTF8Encoding Utf8 { get; } = new(false);

    private static string? GetSyntaxNamespace(SyntaxNode node)
    {
        if (node is NamespaceDeclarationSyntax ns)
        {
            return ns.Name.ToString();
        }
        if (node is FileScopedNamespaceDeclarationSyntax fns)
        {
            return fns.Name.ToString();
        }
        if (node.Parent is null)
        {
            return default;
        }
        return GetSyntaxNamespace(node.Parent);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ProtoInfoSyntaxReceiver receiver || receiver.Matches is null)
        {
            return;
        }
        foreach (var match in receiver.Matches)
        {
            var info = new ProtoInfoParser(match.SemanticModel).ParseInfo(match);
            var code = new ProtoInfoEmitter(info).EmitServiceInfo(GetSyntaxNamespace(match.Cds) ?? "NCoreUtils.Proto.Generated", match.Cds.Identifier.ValueText);
            context.AddSource($"{match.Cds.Identifier.ValueText}.g.cs", SourceText.From(code, Utf8));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(ctx => ctx.AddSource("Attributes.cs", SourceText.From(attributeSource, Utf8)));
        context.RegisterForSyntaxNotifications(() => new ProtoInfoSyntaxReceiver());
    }
}