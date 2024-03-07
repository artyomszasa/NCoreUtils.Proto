using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

public abstract class TypeName
{
    public sealed class GenerationTimeTypeName(string fullName) : TypeName
    {
        private readonly string _fullName = fullName;

        public override bool IsNullableReference => _fullName.EndsWith("?");

        public override string FullName => _fullName;

        public override string JsonContextName => throw new NotSupportedException();
    }

    public sealed class DefinedTypeName(ITypeSymbol type) : TypeName
    {
        public ITypeSymbol Type { get; } = type;

        public override bool IsNullableReference => Type.NullableAnnotation == NullableAnnotation.Annotated;

        public override string FullName => Type.ToFullMaybeNullableName();

        public override string JsonContextName => GetTypeInfoPropertyName(Type);
    }

    // see https://github.com/dotnet/runtime/blob/9b1da975a3a028ae22ce7ffb4ca838dfe34aac59/src/libraries/System.Text.Json/gen/Reflection/TypeExtensions.cs#L58
    public static string GetTypeInfoPropertyName(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Array)
        {
            return GetTypeInfoPropertyName(((IArrayTypeSymbol)type).ElementType) + "Array";
        }
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return type.Name;
        }

        StringBuilder sb = new();

        string name = namedType.Name;

        sb.Append(name);

        foreach (ITypeSymbol genericArg in namedType.TypeArguments)
        {
            sb.Append(GetTypeInfoPropertyName(genericArg));
        }

        return sb.ToString();
    }

    public static implicit operator string(TypeName typeName)
        => typeName.FullName;

    public static TypeName Create(ITypeSymbol type)
        => new DefinedTypeName(type);

    public static TypeName Create(string fullName)
        => new GenerationTimeTypeName(fullName);

    public abstract bool IsNullableReference { get; }

    public abstract string FullName { get; }

    public abstract string JsonContextName { get; }

    public override string ToString()
        => FullName;
}