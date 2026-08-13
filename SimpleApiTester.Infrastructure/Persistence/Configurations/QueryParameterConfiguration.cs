using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Infrastructure.Persistence.Configurations;

public sealed class QueryParameterConfiguration
    : IEntityTypeConfiguration<QueryParameter>
{
    public void Configure(EntityTypeBuilder<QueryParameter> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(500);

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.HasOne(x => x.Operation)
            .WithMany(x => x.QueryParameters)
            .HasForeignKey(x => x.OperationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
