using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Infrastructure.Persistence.Configurations;

public sealed class DataSourceEnvironmentConfiguration : IEntityTypeConfiguration<DataSourceEnvironment>
{
    public void Configure(EntityTypeBuilder<DataSourceEnvironment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BaseUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => new { x.DataSourceId, x.Name })
            .IsUnique();

        builder.HasOne(x => x.DataSource)
            .WithMany(x => x.Environments)
            .HasForeignKey(x => x.DataSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Variables)
            .WithOne(x => x.DataSourceEnvironment)
            .HasForeignKey(x => x.DataSourceEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
