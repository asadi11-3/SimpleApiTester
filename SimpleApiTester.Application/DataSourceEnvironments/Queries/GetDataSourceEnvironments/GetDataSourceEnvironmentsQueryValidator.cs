using FluentValidation;

namespace SimpleApiTester.Application.DataSourceEnvironments.Queries.GetDataSourceEnvironments;

public sealed class GetDataSourceEnvironmentsQueryValidator : AbstractValidator<GetDataSourceEnvironmentsQuery>
{
    public GetDataSourceEnvironmentsQueryValidator()
    {
        RuleFor(x => x.DataSourceId)
            .NotEmpty();
    }
}
