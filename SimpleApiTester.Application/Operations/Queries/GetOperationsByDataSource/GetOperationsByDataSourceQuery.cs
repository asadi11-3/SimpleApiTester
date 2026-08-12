using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.Operations.Queries.GetOperationsByDataSource
{
    public sealed record GetOperationsByDataSourceQuery(Guid DataSourceId)
      : IRequest<IReadOnlyList<OperationResponse>>;
}
