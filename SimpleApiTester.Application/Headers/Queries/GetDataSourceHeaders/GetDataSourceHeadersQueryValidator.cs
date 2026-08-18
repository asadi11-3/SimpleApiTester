using FluentValidation;

namespace SimpleApiTester.Application.Headers.Queries.GetDataSourceHeaders;

public sealed class GetDataSourceHeadersQueryValidator : AbstractValidator<GetDataSourceHeadersQuery>
{
    public GetDataSourceHeadersQueryValidator()
    {
        RuleFor(x => x.DataSourceId)
            .NotEmpty();
    }
}
