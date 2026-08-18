using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources;

public sealed record DataSourceResponse(
    Guid Id,
    string Key,
    bool IsActive);
