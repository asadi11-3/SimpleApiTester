using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Infrastructure.Persistence;

internal static class ProviderModelConfiguration
{
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";
    private const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const string InMemoryProvider = "Microsoft.EntityFrameworkCore.InMemory";

    private const string DefaultTimeoutConstraint =
        "[DefaultTimeoutSeconds] IS NULL OR ([DefaultTimeoutSeconds] >= 1 AND [DefaultTimeoutSeconds] <= 300)";

    private const string AuthenticationShapeConstraint =
        "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) " +
        "OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [ApiKeyLocation] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) " +
        "OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) " +
        "OR ([AuthenticationType] = 'OAuthClientCredentials' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NOT NULL AND [OAuthClientIdSourceType] IS NOT NULL AND [OAuthClientIdSourceKey] IS NOT NULL AND [OAuthClientSecretSourceType] IS NOT NULL AND [OAuthClientSecretSourceKey] IS NOT NULL)";

    private const string AuthenticationSourceTypesConstraint =
        "([ValueSourceType] IS NULL OR [ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))" +
        " AND ([UsernameSourceType] IS NULL OR [UsernameSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([PasswordSourceType] IS NULL OR [PasswordSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))" +
        " AND ([OAuthClientIdSourceType] IS NULL OR [OAuthClientIdSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))" +
        " AND ([OAuthClientSecretSourceType] IS NULL OR [OAuthClientSecretSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))";

    private const string ApiKeyLocationConstraint =
        "([ApiKeyLocation] IS NULL OR [ApiKeyLocation] IN ('Header', 'Query'))";

    private const string HeaderParentConstraint =
        "(([DataSourceId] IS NOT NULL AND [OperationId] IS NULL) OR ([DataSourceId] IS NULL AND [OperationId] IS NOT NULL))";

    private const string HeaderValueSourceConstraint =
        "(([ValueSourceType] = 'General' AND [Value] IS NOT NULL AND [SourceKey] IS NULL) OR ([ValueSourceType] <> 'General' AND [Value] IS NULL AND [SourceKey] IS NOT NULL))";

    public static void ApplyProviderSpecificConfiguration(
        this ModelBuilder modelBuilder,
        string? providerName)
    {
        switch (providerName)
        {
            case SqlServerProvider:
                ConfigureSqlServer(modelBuilder);
                break;
            case PostgreSqlProvider:
                ConfigurePostgreSql(modelBuilder);
                break;
            case InMemoryProvider:
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported Entity Framework provider '{providerName ?? "<none>"}'.");
        }
    }

    private static void ConfigureSqlServer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataSourceEnvironment>()
            .Property(x => x.Name)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<Header>()
            .Property(x => x.Key)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<Variable>()
            .Property(x => x.Key)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<Operation>()
            .Property(x => x.Body)
            .HasColumnType("nvarchar(max)");

        ApplyRelationalConstraints(modelBuilder, static sql => sql);
    }

    private static void ConfigurePostgreSql(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<DataSource>()
            .Property(x => x.Key)
            .HasColumnType("citext");

        modelBuilder.Entity<DataSourceEnvironment>()
            .Property(x => x.Name)
            .HasColumnType("citext");

        modelBuilder.Entity<Header>()
            .Property(x => x.Key)
            .HasColumnType("citext");

        modelBuilder.Entity<Variable>()
            .Property(x => x.Key)
            .HasColumnType("citext");

        modelBuilder.Entity<DataSource>().ToTable(tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "CK_DataSources_Key_Length",
                "char_length(\"Key\") <= 100"));

        modelBuilder.Entity<DataSourceEnvironment>().ToTable(tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "CK_DataSourceEnvironments_Name_Length",
                "char_length(\"Name\") <= 100"));

        modelBuilder.Entity<Header>().ToTable(tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "CK_Headers_Key_Length",
                "char_length(\"Key\") <= 100"));

        modelBuilder.Entity<Variable>().ToTable(tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "CK_Variables_Key_Length",
                "char_length(\"Key\") <= 100"));

        ApplyRelationalConstraints(
            modelBuilder,
            static sql => sql.Replace('[', '"').Replace(']', '"'));
    }

    private static void ApplyRelationalConstraints(
        ModelBuilder modelBuilder,
        Func<string, string> sqlTransform)
    {
        modelBuilder.Entity<DataSource>().ToTable(tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "CK_DataSources_DefaultTimeoutSeconds_Range",
                sqlTransform(DefaultTimeoutConstraint)));

        modelBuilder.Entity<DataSourceAuthentication>().ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_DataSourceAuthentications_AuthShape",
                sqlTransform(AuthenticationShapeConstraint));
            tableBuilder.HasCheckConstraint(
                "CK_DataSourceAuthentications_SourceTypes",
                sqlTransform(AuthenticationSourceTypesConstraint));
            tableBuilder.HasCheckConstraint(
                "CK_DataSourceAuthentications_ApiKeyLocation",
                sqlTransform(ApiKeyLocationConstraint));
        });

        var header = modelBuilder.Entity<Header>();

        header.HasIndex(x => new { x.DataSourceId, x.Key })
            .HasFilter(sqlTransform("[DataSourceId] IS NOT NULL"));

        header.HasIndex(x => new { x.OperationId, x.Key })
            .HasFilter(sqlTransform("[OperationId] IS NOT NULL"));

        header.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Headers_ExactlyOneParent",
                sqlTransform(HeaderParentConstraint));
            tableBuilder.HasCheckConstraint(
                "CK_Headers_ValueSourceShape",
                sqlTransform(HeaderValueSourceConstraint));
        });
    }
}
