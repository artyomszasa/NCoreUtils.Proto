using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Proto;

[Generator]
public class ProtoClientGenerator : ISourceGenerator
{
    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Proto
{
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
internal class ProtoClientAttribute : System.Attribute
{
    public System.Type Info { get; }

    public System.Type JsonSerializerContext { get; }

    public string? Path { get; set; }

    public ProtoClientAttribute(System.Type info, System.Type jsonSerializerContext)
    {
        Info = info ?? throw new System.ArgumentNullException(nameof(info));
        JsonSerializerContext = jsonSerializerContext ?? throw new System.ArgumentNullException(nameof(jsonSerializerContext));
    }
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
        if (context.SyntaxContextReceiver is not ProtoClientSyntaxReceiver receiver || receiver.Matches is null)
        {
            return;
        }
        foreach (var match in receiver.Matches)
        {
            var client = new ProtoClientParser(match.SemanticModel).Parse(match);
            var code = new ProtoClientEmitter(client).EmitClient(GetSyntaxNamespace(match.Cds) ?? "NCoreUtils.Proto.Generated", match.Cds.Identifier.ValueText);
            context.AddSource($"{match.Cds.Identifier.ValueText}.g.cs", SourceText.From(code, Utf8));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(ctx => ctx.AddSource("Attributes.cs", SourceText.From(attributeSource, Utf8)));
        context.RegisterForSyntaxNotifications(() => new ProtoClientSyntaxReceiver());
    }
}