namespace SimpleApiTester.Domain.Entities;

public class DataSource
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public ICollection<Operation> Operations { get; set; } = new List<Operation>();

    public ICollection<Variable> Variables { get; set; } = new List<Variable>();

    public ICollection<Header> Headers { get; set; } = new List<Header>();
}
