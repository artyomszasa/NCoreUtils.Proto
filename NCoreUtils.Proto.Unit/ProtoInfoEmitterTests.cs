using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NCoreUtils.Proto.Internal;
using Xunit;

namespace NCoreUtils.Proto.Unit;

public interface IInfoEmitterFixture
{
    Task<int> AddAsync(int firstValue, int secondValue);
}

[ProtoInfo(typeof(IInfoEmitterFixture), Path = "custom")]
[ProtoMethodInfo(nameof(IInfoEmitterFixture.AddAsync), ParameterNaming = Naming.CamelCase)]
public partial class InfoEmitterFixture { }

public class ProtoInfoEmitterTests
{
    [Fact]
    public void MethodInfoPreservesMetadataAndInterfaceBranches()
    {
        Assert.Equal("math", MathInfo.Path);
        Assert.Equal("AddCAsync", MathInfo.AddCInfo.MethodName);
        Assert.Equal("AddC", MathInfo.AddCInfo.MethodId);
        Assert.Equal("add_c", MathInfo.AddCInfo.Path);
        Assert.Equal(InputType.Json, MathInfo.AddCInfo.Input);
        Assert.Equal(OutputType.Json, MathInfo.AddCInfo.Output);
        Assert.Equal(ErrorType.Default, MathInfo.AddCInfo.Error);
        Assert.Equal(HttpMethod.Default, MathInfo.AddCInfo.HttpMethod);
        Assert.False(MathInfo.AddCInfo.NoReturn);
        Assert.Contains(typeof(IProtoMethodReturn<Task<int>, int>), typeof(MathInfo.AddCInfo).GetInterfaces());
        Assert.Contains(typeof(IProtoMethodInputDto<DtoMathInfoAddCArgs>), typeof(MathInfo.AddCInfo).GetInterfaces());
        Assert.Contains(typeof(IProtoMethodInputDtoIsWrapped), typeof(MathInfo.AddCInfo).GetInterfaces());

        Assert.Equal("IncAsync", MathInfo.IncInfo.MethodName);
        Assert.Equal(InputType.Query, MathInfo.IncInfo.Input);
        Assert.Equal(OutputType.Default, MathInfo.IncInfo.Output);
        Assert.True(MathInfo.IncInfo.NoReturn);
        Assert.Contains(typeof(IProtoMethodVoidReturn<Task>), typeof(MathInfo.IncInfo).GetInterfaces());
        Assert.Equal(8, typeof(MathInfo).GetNestedTypes().Length);

        Assert.Equal("custom", InfoEmitterFixture.Path);
        Assert.Equal(Naming.CamelCase, InfoEmitterFixture.AddInfo.ParameterNaming);
    }

    [Fact]
    public void InputDtosPreserveConstructorPropertiesAndConverter()
    {
        var dto = new DtoMathInfoAddCArgs(1, 2);
        Assert.Equal(1, dto.a);
        Assert.Equal(2, dto.b);
        Assert.Null(typeof(DtoMathInfoAddCArgs).GetProperty(nameof(dto.a))!.SetMethod);
        Assert.Equal(typeof(CustomIntConverter),
            typeof(DtoMathInfoAddCArgs).GetProperty(nameof(dto.b))!
                .GetCustomAttribute<JsonConverterAttribute>()!.ConverterType);

        Assert.Contains(typeof(IProtoMethodInputDto<int>), typeof(MathInfo.IncValueInfo).GetInterfaces());
        Assert.DoesNotContain(typeof(IProtoMethodInputDtoIsWrapped), typeof(MathInfo.IncValueInfo).GetInterfaces());
        Assert.Equal(SingleJsonParameterWrapping.DoNotWrap, MathInfo.IncValueInfo.SingleJsonParameterWrapping);
        Assert.Contains(typeof(IProtoMethodInputDto<DtoMathInfoIncValueWArgs>), typeof(MathInfo.IncValueWInfo).GetInterfaces());
        Assert.Equal(SingleJsonParameterWrapping.Wrap, MathInfo.IncValueWInfo.SingleJsonParameterWrapping);
        Assert.NotNull(new DtoMathInfoIncArgs());
    }

    [Fact]
    public void JsonRootPreservesInputAndResultProperties()
    {
        Assert.Equal(typeof(DtoMathInfoAddCArgs), typeof(JsonRootMathInfo).GetProperty("AddCArgs")!.PropertyType);
        Assert.Equal(typeof(int), typeof(JsonRootMathInfo).GetProperty("IncValueArgs")!.PropertyType);
        Assert.Equal(typeof(int), typeof(JsonRootMathInfo).GetProperty("AddCResult")!.PropertyType);
        Assert.Null(typeof(JsonRootMathInfo).GetProperty("IncArgs"));
        Assert.Null(typeof(JsonRootMathInfo).GetProperty("IncResult"));
        Assert.Null(typeof(JsonRootFMathInfo).GetProperty("AddCArgs"));
        Assert.Equal(typeof(MyData), typeof(JsonRootFMathInfo).GetProperty("OverrideNumArgs")!.PropertyType);
    }
}
