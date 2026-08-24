using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Infrastructure.Persistence.Configurations;

public sealed class DataSourceAuthenticationConfiguration
    : IEntityTypeConfiguration<DataSourceAuthentication>
{
    public void Configure(EntityTypeBuilder<DataSourceAuthentication> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.DataSourceId)
            .IsUnique();

        builder.Property(x => x.AuthenticationType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ValueSourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SourceKey)
            .HasMaxLength(500);

        builder.Property(x => x.ApiKeyHeaderName)
            .HasMaxLength(100);

        builder.Property(x => x.ApiKeyLocation)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.UsernameSourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.UsernameSourceKey)
            .HasMaxLength(500);

        builder.Property(x => x.PasswordSourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.PasswordSourceKey)
            .HasMaxLength(500);

        builder.Property(x => x.OAuthTokenEndpoint)
            .HasMaxLength(2000);

        builder.Property(x => x.OAuthClientIdSourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.OAuthClientIdSourceKey)
            .HasMaxLength(500);

        builder.Property(x => x.OAuthClientSecretSourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.OAuthClientSecretSourceKey)
            .HasMaxLength(500);

        builder.Property(x => x.OAuthScope)
            .HasMaxLength(500);

        builder.ToTable(x => x.HasCheckConstraint(
            "CK_DataSourceAuthentications_AuthShape",
            "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) " +
            "OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [ApiKeyLocation] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) " +
            "OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) " +
            "OR ([AuthenticationType] = 'OAuthClientCredentials' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NOT NULL AND [OAuthClientIdSourceType] IS NOT NULL AND [OAuthClientIdSourceKey] IS NOT NULL AND [OAuthClientSecretSourceType] IS NOT NULL AND [OAuthClientSecretSourceKey] IS NOT NULL)"));

        builder.ToTable(x => x.HasCheckConstraint(
            "CK_DataSourceAuthentications_SourceTypes",
            "([ValueSourceType] IS NULL OR [ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))" +
            " AND ([UsernameSourceType] IS NULL OR [UsernameSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([PasswordSourceType] IS NULL OR [PasswordSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))" +
            " AND ([OAuthClientIdSourceType] IS NULL OR [OAuthClientIdSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))" +
            " AND ([OAuthClientSecretSourceType] IS NULL OR [OAuthClientSecretSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))"));

        builder.ToTable(x => x.HasCheckConstraint(
            "CK_DataSourceAuthentications_ApiKeyLocation",
            "([ApiKeyLocation] IS NULL OR [ApiKeyLocation] IN ('Header', 'Query'))"));
    }
}
