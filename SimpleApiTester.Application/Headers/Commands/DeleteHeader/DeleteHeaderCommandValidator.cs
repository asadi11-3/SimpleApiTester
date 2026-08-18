using FluentValidation;

namespace SimpleApiTester.Application.Headers.Commands.DeleteHeader;

public sealed class DeleteHeaderCommandValidator : AbstractValidator<DeleteHeaderCommand>
{
    public DeleteHeaderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
