using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Queries.GetDataSources
{
    public sealed record GetDataSourcesQuery
    : IRequest<IReadOnlyList<DataSourceResponse>>;
}
