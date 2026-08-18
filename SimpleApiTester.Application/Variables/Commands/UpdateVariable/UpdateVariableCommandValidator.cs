using FluentValidation;

namespace SimpleApiTester.Application.Variables.Commands.UpdateVariable;

public sealed class UpdateVariableCommandValidator : AbstractValidator<UpdateVariableCommand>
{
    public UpdateVariableCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Value)
            .MaximumLength(2000);
    }
}
