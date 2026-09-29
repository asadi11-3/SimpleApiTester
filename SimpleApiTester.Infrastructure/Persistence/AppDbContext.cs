using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
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

        public DbSet<DataSourceAuthentication> DataSourceAuthentications => Set<DataSourceAuthentication>();

        public DbSet<DataSourceEnvironment> DataSourceEnvironments => Set<DataSourceEnvironment>();

        public DbSet<Operation> Operations => Set<Operation>();

        public DbSet<QueryParameter> QueryParameters => Set<QueryParameter>();

        public DbSet<Variable> Variables => Set<Variable>();

        public DbSet<Header> Headers => Set<Header>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);

            modelBuilder.ApplyProviderSpecificConfiguration(Database.ProviderName);
        }
    }
}
