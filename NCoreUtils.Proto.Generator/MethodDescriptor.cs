using System.Collections.Generic;

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
{
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
}