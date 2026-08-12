using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Operations.Persistence;
using SimpleApiTester.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Infrastructure.Persistence
{
    public sealed class AppDbContext
      : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<DataSource> DataSources => Set<DataSource>();

        public DbSet<Operation> Operations => Set<Operation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);
        }
    }
}
