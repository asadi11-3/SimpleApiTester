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
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.SourceKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ApiKeyHeaderName)
            .HasMaxLength(100);

        builder.ToTable(x => x.HasCheckConstraint(
            "CK_DataSourceAuthentications_AuthShape",
            "([AuthenticationType] = 'Bearer' AND [ApiKeyHeaderName] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ApiKeyHeaderName] IS NOT NULL)"));

        builder.ToTable(x => x.HasCheckConstraint(
            "CK_DataSourceAuthentications_ValueSourceType",
            "[ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')"));
    }
}
