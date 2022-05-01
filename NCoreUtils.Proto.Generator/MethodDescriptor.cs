using System.Collections.Generic;

namespace NCoreUtils.Proto;

internal class MethodDescriptor
{
    public string ReturnType { get; }

    public TypeName ReturnValueType { get; }

    public bool NoReturn { get; }

    public AsyncReturnType AsyncReturnType { get; }

    public string MethodName { get; }

    public string MethodId { get; }

    public IReadOnlyList<ParameterDescriptor> Parameters { get; }

    public bool UsesCancellation { get; }

    public string Path { get; }

    public string Verb { get; }

    public ProtoInputType Input { get; }

    public ProtoOutputType Output { get; }

    public ProtoErrorType Error { get; }

    public ProtoNaming? ParameterNaming { get; }

    public TypeName? InputDtoTypeName { get; }

    public MethodDescriptor(
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
        TypeName? inputDtoTypeName)
    {
        ReturnType = returnType;
        ReturnValueType = returnValueType;
        NoReturn = noReturn;
        AsyncReturnType = asyncReturnType;
        MethodName = methodName;
        MethodId = methodId;
        Parameters = parameters;
        UsesCancellation = usesCancellation;
        Path = path;
        Verb = verb;
        Input = input;
        Output = output;
        Error = error;
        ParameterNaming = parameterNaming;
        InputDtoTypeName = inputDtoTypeName;
    }
}