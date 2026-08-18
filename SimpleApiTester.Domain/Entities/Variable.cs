namespace SimpleApiTester.Domain.Entities;

public class Variable
{
    public Guid Id { get; set; }

    public Guid DataSourceEnvironmentId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public bool IsEnabled { get; set; }

    public DataSourceEnvironment DataSourceEnvironment { get; set; } = null!;
}
