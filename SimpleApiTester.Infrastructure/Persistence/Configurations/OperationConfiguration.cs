using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleApiTester.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Infrastructure.Persistence.Configurations
{
    public sealed class OperationConfiguration
    : IEntityTypeConfiguration<Operation>
    {
        public void Configure(EntityTypeBuilder<Operation> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ApiName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Endpoint)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.MethodType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Body)
                .HasColumnType("nvarchar(max)");

            builder.HasMany(x => x.QueryParameters)
                .WithOne(x => x.Operation)
                .HasForeignKey(x => x.OperationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
