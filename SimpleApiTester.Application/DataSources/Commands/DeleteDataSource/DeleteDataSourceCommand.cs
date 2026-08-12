using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Commands.DeleteDataSource
{
    public sealed record DeleteDataSourceCommand(Guid Id)
     : IRequest;
}
