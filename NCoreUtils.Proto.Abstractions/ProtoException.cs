using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Proto;

[Serializable]
public class ProtoException : Exception
{
    public string ErrorCode { get; }

    protected ProtoException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        => ErrorCode = info.GetString(nameof(ErrorCode)) ?? string.Empty;

    public ProtoException(string errorCode, string message)
        : base(message)
        => ErrorCode = errorCode;

    public ProtoException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
        => ErrorCode = errorCode;

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode ?? string.Empty);
    }
}