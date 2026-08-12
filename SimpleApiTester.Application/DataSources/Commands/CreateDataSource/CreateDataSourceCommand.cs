using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using SimpleApiTester.Application.Operations.Persistence;
namespace SimpleApiTester.Application.DataSources.Commands.CreateDataSource
{
    public sealed record CreateDataSourceCommand(
     string Key,
     string BaseUrl
 ) : IRequest<Guid>;

 
}
