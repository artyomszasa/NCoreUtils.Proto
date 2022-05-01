namespace NCoreUtils.Proto;

public enum ProtoInputType { Default = 0, Json = 1, Query = 2, Form = 3, Custom = 255 }

public enum ProtoOutputType { Default = 0, Json = 1 }

public enum ProtoErrorType { Default = 0, Json = 1 }

public enum ProtoNaming { SnakeCase = 0, CamelCase = 1, PascalCase = 2, KebabCase = 3 }