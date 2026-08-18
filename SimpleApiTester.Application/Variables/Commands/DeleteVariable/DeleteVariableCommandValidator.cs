using FluentValidation;

namespace SimpleApiTester.Application.Variables.Commands.DeleteVariable;

public sealed class DeleteVariableCommandValidator : AbstractValidator<DeleteVariableCommand>
{
    public DeleteVariableCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
