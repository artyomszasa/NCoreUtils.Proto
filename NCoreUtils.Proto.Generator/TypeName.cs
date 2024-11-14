using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

#pragma warning disable CS0660 // Equals overridden in derved classes
#pragma warning disable CS0661 // GetHashCode overridden in derved classes
public abstract class TypeName : IEquatable<TypeName>
#pragma warning restore CS0661
#pragma warning restore CS0660
{
    private sealed class GenerationTimeTypeName(string fullName)
        : TypeName
        , IEquatable<GenerationTimeTypeName>
    {
        private bool? _isNullableReference;

        private readonly string _fullName = fullName;

        public override bool IsNullableReference => _isNullableReference ??= _fullName.EndsWith("?");

        public override string FullName => _fullName;

        public override string JsonContextName => throw new NotSupportedException();

        public override int GetHashCode()
            => StringComparer.InvariantCulture.GetHashCode(_fullName);

        public override bool Equals([MaybeNullWhen(true)] object? obj)
            => Equals(obj as GenerationTimeTypeName);

        public bool Equals([MaybeNullWhen(true)] GenerationTimeTypeName? other)
            => other is not null
                && _fullName == other._fullName;
    }

    private sealed class DefinedTypeName(ITypeSymbol type)
        : TypeName
        , IEquatable<DefinedTypeName>
    {
        private bool? _isNullableReference;

        private string? _fullname;

        private string? _jsonContextName;

        private ITypeSymbol Type { get; } = type;

        public override bool IsNullableReference => _isNullableReference ??= Type.NullableAnnotation == NullableAnnotation.Annotated;

        public override string FullName => _fullname ??= Type.ToFullMaybeNullableName();

        public override string JsonContextName => _jsonContextName ??= GetTypeInfoPropertyName(Type);

        public override int GetHashCode()
            => HashCode.Combine(
                IsNullableReference,
                StringComparer.InvariantCulture.GetHashCode(FullName),
                StringComparer.InvariantCulture.GetHashCode(JsonContextName)
            );

        public override bool Equals([MaybeNullWhen(true)] object? obj)
            => Equals(obj as DefinedTypeName);

        public bool Equals([NotNullWhen(true)] DefinedTypeName? other)
            // NOTE: symbol itself may have changed between compilations so we're checking only information used during generation.
            => other is not null
                && IsNullableReference == other.IsNullableReference
                && FullName == other.FullName
                && JsonContextName == other.JsonContextName;
    }

    public static bool operator==(TypeName? a, TypeName? b)
        => a is null
            ? b is null
            : a.Equals(b);

    public static bool operator!=(TypeName? a, TypeName? b)
        => a is null
            ? b is not null
            : !a.Equals(b);

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

    public bool Equals([MaybeNullWhen(true)] TypeName? other) => this switch
    {
        null => false,
        GenerationTimeTypeName gttn => gttn.Equals(other as GenerationTimeTypeName),
        DefinedTypeName dtn => dtn.Equals(other as DefinedTypeName),
        _ => ((object)this).Equals(other)
    };
}