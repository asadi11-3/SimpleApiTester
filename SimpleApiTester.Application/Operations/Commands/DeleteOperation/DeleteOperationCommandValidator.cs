using FluentValidation;

namespace SimpleApiTester.Application.Operations.Commands.DeleteOperation;

public sealed class DeleteOperationCommandValidator
    : AbstractValidator<DeleteOperationCommand>
{
    public DeleteOperationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
