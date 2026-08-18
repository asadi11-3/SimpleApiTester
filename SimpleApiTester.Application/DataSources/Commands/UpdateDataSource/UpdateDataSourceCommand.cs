using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Commands.UpdateDataSource
{
    public sealed record UpdateDataSourceCommand(
     Guid Id,
     string Key,
     bool IsActive
  ) : IRequest;
}
