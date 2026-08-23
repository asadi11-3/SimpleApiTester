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

        builder.ToTable(x => x.HasCheckConstraint(
            "CK_DataSourceAuthentications_AuthShape",
            "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) " +
            "OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) " +
            "OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL)"));

        builder.ToTable(x => x.HasCheckConstraint(
            "CK_DataSourceAuthentications_SourceTypes",
            "([ValueSourceType] IS NULL OR [ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))" +
            " AND ([UsernameSourceType] IS NULL OR [UsernameSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([PasswordSourceType] IS NULL OR [PasswordSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))"));
    }
}
