using FluentValidation;

namespace SimpleApiTester.Application.Variables.Queries.GetVariablesByEnvironment;

public sealed class GetVariablesByEnvironmentQueryValidator : AbstractValidator<GetVariablesByEnvironmentQuery>
{
    public GetVariablesByEnvironmentQueryValidator()
    {
        RuleFor(x => x.DataSourceEnvironmentId)
            .NotEmpty();
    }
}
