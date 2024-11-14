using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Proto;

internal class MethodDescriptor(
    string returnType,
    TypeName returnValueType,
    bool noReturn,
    AsyncReturnType asyncReturnType,
    string methodName,
    string methodId,
    IReadOnlyList<ParameterDescriptor> parameters,
    bool usesCancellation,
    string path,
    string verb,
    ProtoInputType input,
    ProtoOutputType output,
    ProtoErrorType error,
    ProtoNaming? parameterNaming,
    ProtoSingleJsonParameterWrapping singleJsonParameterWrapping,
    ProtoHttpMethod httpMethod,
    TypeName? inputDtoTypeName)
    : IEquatable<MethodDescriptor>
{
    private static bool ParametersAreEqual(IReadOnlyList<ParameterDescriptor> a, IReadOnlyList<ParameterDescriptor> b)
    {
        if (a is null)
        {
            return b is null;
        }
        if (b is null || a.Count != b.Count)
        {
            return false;
        }
        for (var i = 0; i < a.Count; ++i)
        {
            if (!a[i].Equals(b[i]))
            {
                return false;
            }
        }
        return true;
    }

    public string ReturnType { get; } = returnType;

    public TypeName ReturnValueType { get; } = returnValueType;

    public bool NoReturn { get; } = noReturn;

    public AsyncReturnType AsyncReturnType { get; } = asyncReturnType;

    public string MethodName { get; } = methodName;

    public string MethodId { get; } = methodId;

    public IReadOnlyList<ParameterDescriptor> Parameters { get; } = parameters;

    public bool UsesCancellation { get; } = usesCancellation;

    public string Path { get; } = path;

    public string Verb { get; } = verb;

    public ProtoInputType Input { get; } = input;

    public ProtoOutputType Output { get; } = output;

    public ProtoErrorType Error { get; } = error;

    public ProtoNaming? ParameterNaming { get; } = parameterNaming;

    public ProtoSingleJsonParameterWrapping SingleJsonParameterWrapping { get; } = singleJsonParameterWrapping;

    public ProtoHttpMethod HttpMethod { get; } = httpMethod;

    public TypeName? InputDtoTypeName { get; } = inputDtoTypeName;

    public bool Equals([NotNullWhen(true)] MethodDescriptor? other)
        => other is not null
            && ReturnType == other.ReturnType
            && ReturnValueType == other.ReturnValueType
            && NoReturn == other.NoReturn
            && AsyncReturnType == other.AsyncReturnType
            && MethodName == other.MethodName
            && MethodId == other.MethodId
            && ParametersAreEqual(Parameters, other.Parameters)
            && UsesCancellation == other.UsesCancellation
            && Path == other.Path
            && Verb == other.Verb
            && Input == other.Input
            && Output == other.Output
            && Error == other.Error
            && ParameterNaming == other.ParameterNaming
            && SingleJsonParameterWrapping == other.SingleJsonParameterWrapping
            && HttpMethod == other.HttpMethod
            && InputDtoTypeName == other.InputDtoTypeName;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => Equals(obj as MethodDescriptor);

    public override int GetHashCode() => HashCode.Combine(
        ReturnValueType,
        AsyncReturnType,
        MethodId,
        Path,
        Verb,
        HashCode.Combine(
            Input,
            Output,
            Error,
            ParameterNaming,
            SingleJsonParameterWrapping,
            HttpMethod
        )
    );
}