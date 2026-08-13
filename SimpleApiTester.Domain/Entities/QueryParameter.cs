namespace SimpleApiTester.Domain.Entities;

public class QueryParameter
{
    public Guid Id { get; set; }

    public Guid OperationId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public bool IsEnabled { get; set; }

    public Operation Operation { get; set; } = null!;
}
