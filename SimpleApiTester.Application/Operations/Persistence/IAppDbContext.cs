using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.Operations.Persistence;

public interface IAppDbContext
{
    DbSet<DataSource> DataSources { get; }

    DbSet<Operation> Operations { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}