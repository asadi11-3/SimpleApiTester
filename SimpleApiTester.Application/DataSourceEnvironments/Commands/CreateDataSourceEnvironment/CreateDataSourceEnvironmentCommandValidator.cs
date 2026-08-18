using FluentValidation;
using SimpleApiTester.Application.DataSourceEnvironments;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.CreateDataSourceEnvironment;

public sealed class CreateDataSourceEnvironmentCommandValidator : AbstractValidator<CreateDataSourceEnvironmentCommand>
{
    public CreateDataSourceEnvironmentCommandValidator()
    {
        RuleFor(x => x.DataSourceId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .MaximumLength(500)
            .Must(DataSourceEnvironmentRules.IsValidBaseUrl)
            .WithMessage("BaseUrl must be a valid absolute URL.");
    }
}
