using FluentValidation;

namespace SimpleApiTester.Application.Variables.Commands.CreateVariable;

public sealed class CreateVariableCommandValidator : AbstractValidator<CreateVariableCommand>
{
    public CreateVariableCommandValidator()
    {
        RuleFor(x => x.DataSourceEnvironmentId)
            .NotEmpty();

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Value)
            .MaximumLength(2000);
    }
}
