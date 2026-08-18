using Microsoft.Extensions.Configuration;
using SimpleApiTester.Domain.Enum;
using SimpleApiTester.Infrastructure.Services;

namespace SimpleApiTester.Tests.Infrastructure;

public sealed class ExternalHeaderValueResolverTests
{
    [Fact]
    public async Task ResolveAsync_ForUserSecretSource_ReadsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secrets:ApiKey"] = "secret-123"
            })
            .Build();

        var resolver = new ExternalHeaderValueResolver(configuration);

        var result = await resolver.ResolveAsync(
            HeaderValueSourceType.UserSecret,
            "Secrets:ApiKey",
            CancellationToken.None);

        Assert.Equal("secret-123", result);
    }

    [Fact]
    public async Task ResolveAsync_ForEnvironmentVariableSource_ReadsFromProcessEnvironment()
    {
        const string variableName = "SIMPLE_API_TESTER_TEST_TOKEN";

        Environment.SetEnvironmentVariable(variableName, "env-456");

        try
        {
            var configuration = new ConfigurationBuilder().Build();
            var resolver = new ExternalHeaderValueResolver(configuration);

            var result = await resolver.ResolveAsync(
                HeaderValueSourceType.EnvironmentVariable,
                variableName,
                CancellationToken.None);

            Assert.Equal("env-456", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
