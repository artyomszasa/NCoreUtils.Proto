namespace NCoreUtils.Proto;

public enum InputType { Default = 0, Json = 1, Query = 2, Form = 3, Custom = 255 }

public enum OutputType { Default = 0, Json = 1, Custom = 255 }

public enum ErrorType { Default = 0, Json = 1, Custom = 255 }

public enum Naming { SnakeCase = 0, CamelCase = 1, PascalCase = 2, KebabCase = 3 }

public enum HttpMethod
{
    Default = 0,
    Get = 1,
    Post = 2,
    Put = 3,
    Delete = 4,
    Patch = 5
}