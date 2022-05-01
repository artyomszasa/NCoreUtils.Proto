using System;

namespace NCoreUtils.Proto;

public class ProtoClientInvalidInfoException : Exception
{
    public ProtoClientInvalidInfoException(string message)
        : base(message)
    { }

    public ProtoClientInvalidInfoException(string message, Exception innerException)
        : base(message, innerException)
    { }
}