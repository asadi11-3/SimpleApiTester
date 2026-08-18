using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleApiTester.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Infrastructure.Persistence.Configurations
{
    public sealed class DataSourceConfiguration
    : IEntityTypeConfiguration<DataSource>
    {
        public void Configure(EntityTypeBuilder<DataSource> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(x => x.Key)
                .IsUnique();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.HasOne(x => x.Authentication)
                .WithOne(x => x.DataSource)
                .HasForeignKey<DataSourceAuthentication>(x => x.DataSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Operations)
                .WithOne(x => x.DataSource)
                .HasForeignKey(x => x.DataSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Environments)
                .WithOne(x => x.DataSource)
                .HasForeignKey(x => x.DataSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Headers)
                .WithOne(x => x.DataSource)
                .HasForeignKey(x => x.DataSourceId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
