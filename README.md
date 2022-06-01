## Single JSON parameter wrapping

By default single JSON parameter is not wrapped into a DTO. This behaviour can be changed by setting optional `SingleJsonParameterWrapping` parameter to `Wrap`.

Consider the following code:
```csharp
public class MyInput
{
    public int NumValue { get; }

    public string StrValue { get; }

    public MyInput(int numValue, string strValue)
    {
        NumValue = numValue;
        StrValue = strValue;
    }
}

// ...

public interface IMyService
{
    Task MyMethodAsync(
        MyInput input,
        CancellationToken cancellationToken = default
    );
}

// ...

[ProtoInfo(typeof(IMyService), Input = InputType.Json)]
public class MyServiceInfo { }
```

Default behaviour is not to wrap a single parameter, i.e. the body of the request will be the following:

```json
{
    "numValue": 0,
    "strValue": "some string"
}
```

But if the `SingleJsonParameterWrapping` parameter is set to `Wrap` for service:
```csharp
[ProtoInfo(typeof(IMyService), Input = InputType.Json,
    SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
public class MyServiceInfo { }
```
or for a single method:
```csharp
[ProtoInfo(typeof(IMyService), Input = InputType.Json)]
[ProtoMethodInfo(nameof(IMyService.MyMethodAsync),
    SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
public class MyServiceInfo { }
```
then parameter is wrapped unconditionally and the body of the request will look like this:

```json
{
    "input": {
        "numValue": 0,
        "strValue": "some string"
    }
}
```