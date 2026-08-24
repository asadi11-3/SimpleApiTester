using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Domain.Entities;

public class DataSourceAuthentication
{
    public Guid Id { get; set; }

    public Guid DataSourceId { get; set; }

    public AuthenticationType AuthenticationType { get; set; }

    public HeaderValueSourceType? ValueSourceType { get; set; }

    public string? SourceKey { get; set; }

    public string? ApiKeyHeaderName { get; set; }

    public ApiKeyLocation? ApiKeyLocation { get; set; }

    public HeaderValueSourceType? UsernameSourceType { get; set; }

    public string? UsernameSourceKey { get; set; }

    public HeaderValueSourceType? PasswordSourceType { get; set; }

    public string? PasswordSourceKey { get; set; }

    public string? OAuthTokenEndpoint { get; set; }

    public HeaderValueSourceType? OAuthClientIdSourceType { get; set; }

    public string? OAuthClientIdSourceKey { get; set; }

    public HeaderValueSourceType? OAuthClientSecretSourceType { get; set; }

    public string? OAuthClientSecretSourceKey { get; set; }

    public string? OAuthScope { get; set; }

    public DataSource DataSource { get; set; } = null!;
}
