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

    }
}
