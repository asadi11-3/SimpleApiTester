using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Infrastructure.Persistence.Configurations;

public sealed class VariableConfiguration : IEntityTypeConfiguration<Variable>
{
    public void Configure(EntityTypeBuilder<Variable> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS")
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(2000);

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.HasIndex(x => new { x.DataSourceId, x.Key })
            .IsUnique();

        builder.HasOne(x => x.DataSource)
            .WithMany(x => x.Variables)
            .HasForeignKey(x => x.DataSourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
