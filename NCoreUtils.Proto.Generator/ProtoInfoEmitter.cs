using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Proto;

internal class ProtoInfoEmitter(ProtoServiceInfo info)
{
    private ProtoServiceInfo Info { get; } = info ?? throw new ArgumentNullException(nameof(info));

    private static TypeSyntax GenericType(string qualifier, string name, params string[] arguments)
        => QualifiedName(
            ParseName(qualifier),
            GenericName(Identifier(name)).WithTypeArgumentList(
                TypeArgumentList(SeparatedList(arguments.Select(argument => ParseTypeName(argument)))))
        );

    private static ExpressionSyntax EnumValue(string typeName, string value)
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ParseName(typeName),
            IdentifierName(value)
        );

    private static FieldDeclarationSyntax Constant(string typeName, string name, ExpressionSyntax value)
        => FieldDeclaration(
            VariableDeclaration(ParseTypeName(typeName))
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(Identifier(name)).WithInitializer(EqualsValueClause(value))
                ))
        ).AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword));

    private static FieldDeclarationSyntax StringConstant(string name, string value)
        => Constant("string", name, LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value)));

    private static FieldDeclarationSyntax EnumConstant(string typeName, string name, string value)
        => Constant(typeName, name, EnumValue(typeName, value));

    private static ClassDeclarationSyntax DoEmitMethodInfo(MethodDescriptor desc)
    {
        var bases = new List<BaseTypeSyntax>
        {
            SimpleBaseType(ParseTypeName("global::NCoreUtils.Proto.Internal.ProtoMethodInfo")),
            SimpleBaseType(desc.NoReturn
                ? GenericType("global::NCoreUtils.Proto.Internal", "IProtoMethodVoidReturn", desc.ReturnType)
                : GenericType("global::NCoreUtils.Proto.Internal", "IProtoMethodReturn", desc.ReturnType, desc.ReturnValueType.FullName))
        };
        if (desc.InputDtoTypeName is not null)
        {
            bases.Add(SimpleBaseType(GenericType(
                "global::NCoreUtils.Proto.Internal", "IProtoMethodInputDto", desc.InputDtoTypeName.FullName)));
            if (desc.InputDtoTypeName.IsWrapper)
            {
                bases.Add(SimpleBaseType(ParseTypeName("global::NCoreUtils.Proto.Internal.IProtoMethodInputDtoIsWrapped")));
            }
        }

        var members = new List<MemberDeclarationSyntax>
        {
            StringConstant("MethodName", desc.MethodName),
            StringConstant("MethodId", desc.MethodId),
            EnumConstant("global::NCoreUtils.Proto.InputType", "Input", desc.Input.ToString()),
            EnumConstant("global::NCoreUtils.Proto.OutputType", "Output", desc.Output.ToString()),
            EnumConstant("global::NCoreUtils.Proto.ErrorType", "Error", desc.Error.ToString()),
            EnumConstant("global::NCoreUtils.Proto.HttpMethod", "HttpMethod", desc.HttpMethod.ToString())
        };
        if (desc.ParameterNaming.HasValue)
        {
            members.Add(EnumConstant("global::NCoreUtils.Proto.Naming", "ParameterNaming", desc.ParameterNaming.Value.ToString()));
        }
        members.Add(EnumConstant(
            "global::NCoreUtils.Proto.SingleJsonParameterWrapping", "SingleJsonParameterWrapping", desc.SingleJsonParameterWrapping.ToString()));
        members.Add(StringConstant("Path", desc.Path));
        members.Add(Constant("bool", "NoReturn", LiteralExpression(
            desc.NoReturn ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)));

        return ClassDeclaration(desc.MethodId + "Info")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .WithBaseList(BaseList(SeparatedList(bases)))
            .WithMembers(List(members));
    }

    private static ConcurrentDictionary<MethodDescriptor, ClassDeclarationSyntax> MethodInfoCache { get; } = new();

    private static ClassDeclarationSyntax EmitMethodInfo(MethodDescriptor desc)
        => MethodInfoCache.GetOrAdd(desc, DoEmitMethodInfo);

    private static PropertyDeclarationSyntax EmitProperty(ParameterDescriptor parameter)
    {
        var property = PropertyDeclaration(ParseTypeName(parameter.TypeName), parameter.Name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(AccessorList(SingletonList(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            )));
        if (parameter.ConverterTypeFullName is not null)
        {
            property = property.AddAttributeLists(AttributeList(SingletonSeparatedList(
                Attribute(ParseName("System.Text.Json.Serialization.JsonConverterAttribute"))
                    .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                        AttributeArgument(TypeOfExpression(ParseTypeName(parameter.ConverterTypeFullName)))
                    )))
            )));
        }
        return property;
    }

    private static ClassDeclarationSyntax? EmitInputDto(MethodDescriptor desc)
    {
        if (desc.SingleJsonParameterWrapping == ProtoSingleJsonParameterWrapping.DoNotWrap && desc.Parameters.Count == 1)
        {
            return null;
        }

        var name = desc.InputDtoTypeName!.FullName;
        var parameters = desc.Parameters.Select(parameter =>
            Parameter(Identifier(parameter.Name)).WithType(ParseTypeName(parameter.TypeName)));
        var assignments = desc.Parameters.Select(parameter => (StatementSyntax)ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(parameter.Name)),
                IdentifierName(parameter.Name))));
        var constructor = ConstructorDeclaration(name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithBody(Block(assignments));

        return ClassDeclaration(name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithMembers(List<MemberDeclarationSyntax>(
                desc.Parameters.Select(parameter => (MemberDeclarationSyntax)EmitProperty(parameter))
                    .Append(constructor)));
    }

    private static PropertyDeclarationSyntax RootProperty(string typeName, string name)
        => PropertyDeclaration(ParseTypeName(typeName), name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(AccessorList(List(new[]
            {
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            })))
            .WithInitializer(EqualsValueClause(PostfixUnaryExpression(
                SyntaxKind.SuppressNullableWarningExpression,
                LiteralExpression(SyntaxKind.DefaultLiteralExpression))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private ClassDeclarationSyntax EmitRootSerializationClass(string name)
        => ClassDeclaration("JsonRoot" + name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithMembers(List<MemberDeclarationSyntax>(
                Info.Methods.Where(method => method.Input == ProtoInputType.Json)
                    .Select(method => (MemberDeclarationSyntax)RootProperty(method.InputDtoTypeName!.FullName, method.MethodId + "Args"))
                    .Concat(Info.Methods.Where(method => !method.NoReturn && method.Output == ProtoOutputType.Json)
                        .Select(method => (MemberDeclarationSyntax)RootProperty(method.ReturnValueType.FullName, method.MethodId + "Result")))));

    public CompilationUnitSyntax EmitServiceInfo(string @namespace, string name)
    {
        var service = ClassDeclaration(name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(
                GenericType("global::NCoreUtils.Proto.Internal", "ProtoServiceInfo", Info.TargetFullName)))))
            .WithMembers(List<MemberDeclarationSyntax>(
                Info.Methods.Select(method => (MemberDeclarationSyntax)EmitMethodInfo(method))
                    .Append(StringConstant("Path", Info.Path))));

        var members = new List<MemberDeclarationSyntax> { service };
        members.AddRange(Info.Methods.Select(EmitInputDto).Where(dto => dto is not null)!);
        members.Add(EmitRootSerializationClass(name));

        return CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                NamespaceDeclaration(ParseName(@namespace))
                    .WithLeadingTrivia(ParseLeadingTrivia("#nullable enable\n"))
                    .WithMembers(List(members))));
    }
}
