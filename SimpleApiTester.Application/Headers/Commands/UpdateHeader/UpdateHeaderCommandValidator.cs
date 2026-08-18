using FluentValidation;

namespace SimpleApiTester.Application.Headers.Commands.UpdateHeader;

public sealed class UpdateHeaderCommandValidator : AbstractValidator<UpdateHeaderCommand>
{
    public UpdateHeaderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        Include(new HeaderCommandValidatorBase<UpdateHeaderCommand>());
    }
}
