using System.Linq;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Proto;

internal abstract class ProtoConsumerParser : ProtoParser
{
    protected ProtoConsumerParser(SemanticModel semanticModel) : base(semanticModel) { }

    protected ProtoServiceInfo ParseInfoType(ITypeSymbol infoType, string? path)
    {
        var interfaceType = infoType.BaseType?.TypeArguments[0] ?? throw new ProtoClientInvalidInfoException($"No interface type defined for {infoType.Name}");
        var rootPath = infoType.GetMembers().OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.Name == "Path")
                ?.ConstantValue
                as string
                ?? throw new ProtoClientInvalidInfoException($"Path is not defined in {infoType}.");
        var ms = infoType
            .GetMembers()
            .OfType<INamedTypeSymbol>()
            .Where(e => e.BaseType?.Name == "ProtoMethodInfo")
            .Select(e =>
            {
                var (noReturn, returnType, returnValueType) = e.AllInterfaces.TryGetFirst(i => i.Name == "IProtoMethodReturn", out var ret)
                    ? (false, ret.TypeArguments[0], ret.TypeArguments[1])
                    : e.AllInterfaces.TryGetFirst(i => i.Name == "IProtoMethodVoidReturn", out var voidRet)
                        ? (true, voidRet.TypeArguments[0], SemanticModel.Compilation.GetSpecialType(SpecialType.System_Void))
                        : throw new ProtoClientInvalidInfoException($"Method {e.Name} has not return type defined.");
                var methodName = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "MethodName")
                    ?.ConstantValue
                    as string
                    ?? throw new ProtoClientInvalidInfoException($"Method {e.Name} has no method name defined");
                var asyncReturnType = returnType switch
                {
                    INamedTypeSymbol nts when Eqx(nts.ConstructedFrom, TypeTaskOf) => AsyncReturnType.Task,
                    INamedTypeSymbol nts when Eqx(nts.ConstructedFrom, TypeValueTaskOf) => AsyncReturnType.ValueTask,
                    var ts when Eqx(ts, TypeTask) => AsyncReturnType.Task,
                    var ts when Eqx(ts, TypeValueTask) => AsyncReturnType.ValueTask,
                    var ts => throw new ProtoClientInvalidInfoException($"Method {methodName} has unsupported return type.")
                };
                var methodId = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "MethodId")
                    ?.ConstantValue
                    as string
                    ?? throw new ProtoClientInvalidInfoException($"Method {e.Name} has no method id defined");
                var path = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "Path")
                    ?.ConstantValue
                    as string
                    ?? throw new ProtoClientInvalidInfoException($"Method {e.Name} has no path defined");
                var targetMethod = interfaceType.GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => m.Name == methodName);
                var parameterNaming = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "ParameterNaming")
                    ?.ConstantValue
                    ?.UnboxEnum<ProtoNaming>();
                var (usesCancellation, parameters) = GetParameters(targetMethod, parameterNaming);
                var input = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "Input")
                    ?.ConstantValue
                    ?.UnboxEnum<ProtoInputType>()
                    ?? throw new ProtoClientInvalidInfoException($"Method {e.Name} has no input defined");
                var output = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "Output")
                    ?.ConstantValue
                    ?.UnboxEnum<ProtoOutputType>()
                    ?? throw new ProtoClientInvalidInfoException($"Method {e.Name} has no output defined");
                var error = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "Error")
                    ?.ConstantValue
                    ?.UnboxEnum<ProtoErrorType>()
                    ?? throw new ProtoClientInvalidInfoException($"Method {e.Name} has no error defined");
                var sjaw = e.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == "SingleJsonParameterWrapping")
                    ?.ConstantValue
                    ?.UnboxEnum<ProtoSingleJsonParameterWrapping>()
                    ?? throw new ProtoClientInvalidInfoException($"Method {e.Name} has no single json parameter wrapping defined");
                var verb = input switch
                {
                    ProtoInputType.Query => "Get",
                    _ => "Post"
                };
                var inputDtoType = e.AllInterfaces.TryGetFirst(i => i.Name == "IProtoMethodInputDto", out var dto)
                    ? dto.TypeArguments[0]
                    : default;
                return new MethodDescriptor(
                    returnType: returnType.ToFullMaybeNullableName(),
                    returnValueType: TypeName.Create(returnValueType),
                    noReturn: noReturn,
                    asyncReturnType: asyncReturnType,
                    methodName: methodName,
                    methodId: methodId,
                    parameters: parameters,
                    usesCancellation: usesCancellation,
                    path: path,
                    verb: verb,
                    input: input,
                    output: output,
                    error: error,
                    parameterNaming: parameterNaming,
                    singleJsonParameterWrapping: sjaw,
                    inputDtoTypeName: inputDtoType is null ? default : TypeName.Create(inputDtoType)
                );
            })
            .ToList();

        return new ProtoServiceInfo(
            target: interfaceType,
            path: path ?? rootPath,
            methods: ms
        );
    }
}