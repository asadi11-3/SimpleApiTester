using FluentValidation;

namespace SimpleApiTester.Application.Variables.Queries.GetVariablesByDataSource;

public sealed class GetVariablesByDataSourceQueryValidator : AbstractValidator<GetVariablesByDataSourceQuery>
{
    public GetVariablesByDataSourceQueryValidator()
    {
        RuleFor(x => x.DataSourceId)
            .NotEmpty();
    }
}
