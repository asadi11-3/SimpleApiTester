using FluentValidation;
using SimpleApiTester.Application.DataSourceEnvironments;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.UpdateDataSourceEnvironment;

public sealed class UpdateDataSourceEnvironmentCommandValidator : AbstractValidator<UpdateDataSourceEnvironmentCommand>
{
    public UpdateDataSourceEnvironmentCommandValidator()
    {
        RuleFor(x => x.Id)
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
