using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Proto;

[Generator]
public class ProtoImplGenerator : ISourceGenerator
{
    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Proto
{
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
internal class ProtoServiceAttribute : System.Attribute
{
    public System.Type Info { get; }

    public System.Type JsonSerializerContext { get; }

    public string? Path { get; set; }

    public System.Type? ImplementationFactory { get; set; }

    public ProtoServiceAttribute(System.Type info, System.Type jsonSerializerContext)
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
        if (context.SyntaxContextReceiver is not ProtoImplSyntaxReceiver receiver || receiver.Matches is null)
        {
            return;
        }
        foreach (var match in receiver.Matches)
        {
            var service = new ProtoImplParser(match.SemanticModel).Parse(match);
            var code = new ProtoImplEmitter(service).EmitImpl(GetSyntaxNamespace(match.Cds) ?? "NCoreUtils.Proto.Generated", "Proto" + match.Cds.Identifier.ValueText + "Implementation");
            context.AddSource($"{match.Cds.Identifier.ValueText}.g.cs", SourceText.From(code, Utf8));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(ctx => ctx.AddSource("Attributes.cs", SourceText.From(attributeSource, Utf8)));
        context.RegisterForSyntaxNotifications(() => new ProtoImplSyntaxReceiver());
    }
}