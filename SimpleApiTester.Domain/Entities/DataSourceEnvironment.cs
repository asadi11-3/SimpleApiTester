namespace SimpleApiTester.Domain.Entities;

public sealed class DataSourceEnvironment
{
    public Guid Id { get; set; }

    public Guid DataSourceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DataSource DataSource { get; set; } = null!;

    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
}
