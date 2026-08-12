using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Queries.GetById
{
    public sealed record GetDataSourceByIdQuery(Guid Id)
     : IRequest<DataSourceResponse>;
}
