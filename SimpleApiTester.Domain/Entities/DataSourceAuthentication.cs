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

    public HeaderValueSourceType? UsernameSourceType { get; set; }

    public string? UsernameSourceKey { get; set; }

    public HeaderValueSourceType? PasswordSourceType { get; set; }

    public string? PasswordSourceKey { get; set; }

    public DataSource DataSource { get; set; } = null!;
}
