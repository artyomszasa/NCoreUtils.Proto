namespace NCoreUtils.Proto;

public interface IEndpointConfiguration
{
    string? HttpClient { get; }

    string Endpoint { get; }
}