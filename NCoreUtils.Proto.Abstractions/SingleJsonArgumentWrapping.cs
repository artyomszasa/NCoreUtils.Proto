namespace NCoreUtils.Proto;

public enum SingleJsonParameterWrapping
{
    /// <summary>
    /// Do not wrap single JSON parameter, i.e. parameer name is ignored and its type is used as a payload type.
    /// </summary>
    DoNotWrap = 0,

    /// <summary>
    /// Do wrap JSON input parameters even if method receives a single parameter.
    Wrap = 1
}