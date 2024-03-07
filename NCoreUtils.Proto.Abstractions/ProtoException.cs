using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Proto;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class ProtoException : Exception
{
    public string ErrorCode { get; }

#if !NET8_0_OR_GREATER
    protected ProtoException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        => ErrorCode = info.GetString(nameof(ErrorCode)) ?? string.Empty;
#endif

    public ProtoException(string errorCode, string message)
        : base(message)
        => ErrorCode = errorCode;

    public ProtoException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
        => ErrorCode = errorCode;

#if !NET8_0_OR_GREATER
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode ?? string.Empty);
    }
#endif
}