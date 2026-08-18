using FluentValidation;

namespace SimpleApiTester.Application.Headers.Commands.CreateOperationHeader;

public sealed class CreateOperationHeaderCommandValidator : AbstractValidator<CreateOperationHeaderCommand>
{
    public CreateOperationHeaderCommandValidator()
    {
        Include(new HeaderCommandValidatorBase<CreateOperationHeaderCommand>());

        RuleFor(x => x.OperationId)
            .NotEmpty();
    }
}
