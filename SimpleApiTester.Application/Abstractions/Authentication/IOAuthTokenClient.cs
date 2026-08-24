namespace SimpleApiTester.Application.Abstractions.Authentication;

public interface IOAuthTokenClient
{
    Task<OAuthTokenClientResult> RequestClientCredentialsTokenAsync(
        OAuthTokenRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public sealed record OAuthTokenRequest(
    string TokenEndpoint,
    string ClientId,
    string ClientSecret,
    string? Scope);

public sealed record OAuthTokenClientResult(
    string? AccessToken,
    string? ErrorMessage);
