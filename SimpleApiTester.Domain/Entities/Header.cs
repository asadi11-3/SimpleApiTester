using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Domain.Entities;

public class Header
{
    public Guid Id { get; set; }

    public Guid? DataSourceId { get; set; }

    public Guid? OperationId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public HeaderValueSourceType ValueSourceType { get; set; }

    public string? SourceKey { get; set; }

    public bool IsEnabled { get; set; }

    public DataSource? DataSource { get; set; }

    public Operation? Operation { get; set; }
}
