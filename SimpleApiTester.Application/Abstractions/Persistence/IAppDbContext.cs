using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<DataSource> DataSources { get; }

    DbSet<Operation> Operations { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}