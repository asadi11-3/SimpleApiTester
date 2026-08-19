using SimpleApiTester.Application.DataSources;

namespace SimpleApiTester.Application.Abstractions.Http;

public interface IDataSourceConnectionTester
{
    Task<TestDataSourceConnectionResponse> TestConnectionAsync(string baseUrl, TimeSpan timeout, CancellationToken cancellationToken);
}
