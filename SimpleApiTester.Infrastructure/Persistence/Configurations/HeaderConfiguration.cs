using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Infrastructure.Persistence.Configurations;

public sealed class HeaderConfiguration : IEntityTypeConfiguration<Header>
{
    public void Configure(EntityTypeBuilder<Header> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS")
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(2000);

        builder.Property(x => x.ValueSourceType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.SourceKey)
            .HasMaxLength(500);

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.HasIndex(x => new { x.DataSourceId, x.Key })
            .IsUnique()
            .HasFilter("[DataSourceId] IS NOT NULL");

        builder.HasIndex(x => new { x.OperationId, x.Key })
            .IsUnique()
            .HasFilter("[OperationId] IS NOT NULL");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Headers_ExactlyOneParent",
                "(([DataSourceId] IS NOT NULL AND [OperationId] IS NULL) OR ([DataSourceId] IS NULL AND [OperationId] IS NOT NULL))");

            tableBuilder.HasCheckConstraint(
                "CK_Headers_ValueSourceShape",
                "(([ValueSourceType] = 'General' AND [Value] IS NOT NULL AND [SourceKey] IS NULL) OR ([ValueSourceType] <> 'General' AND [Value] IS NULL AND [SourceKey] IS NOT NULL))");
        });

        builder.HasOne(x => x.DataSource)
            .WithMany(x => x.Headers)
            .HasForeignKey(x => x.DataSourceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Operation)
            .WithMany(x => x.Headers)
            .HasForeignKey(x => x.OperationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
