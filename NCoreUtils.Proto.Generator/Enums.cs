namespace NCoreUtils.Proto;

public enum ProtoInputType { Default = 0, Json = 1, Query = 2, Form = 3, Custom = 255 }

public enum ProtoOutputType { Default = 0, Json = 1, Custom = 255 }

public enum ProtoErrorType { Default = 0, Json = 1, Custom = 255 }

public enum ProtoNaming { SnakeCase = 0, CamelCase = 1, PascalCase = 2, KebabCase = 3 }

public enum ProtoSingleJsonParameterWrapping
{
    /// <summary>
    /// Do not wrap single JSON parameter, i.e. parameer name is ignored and its type is used as a payload type.
    /// </summary>
    DoNotWrap = 0,

    /// <summary>
    /// Do wrap JSON input parameters even if method receives a single parameter.
    Wrap = 1
}