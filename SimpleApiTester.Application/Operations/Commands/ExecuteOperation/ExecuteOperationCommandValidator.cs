using FluentValidation;

namespace SimpleApiTester.Application.Operations.Commands.ExecuteOperation;

public sealed class ExecuteOperationCommandValidator
    : AbstractValidator<ExecuteOperationCommand>
{
    public ExecuteOperationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.EnvironmentId)
            .NotEmpty();
    }
}
