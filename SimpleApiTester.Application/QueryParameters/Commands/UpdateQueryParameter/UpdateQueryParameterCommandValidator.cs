using FluentValidation;

namespace SimpleApiTester.Application.QueryParameters.Commands.UpdateQueryParameter;

public sealed class UpdateQueryParameterCommandValidator
    : AbstractValidator<UpdateQueryParameterCommand>
{
    public UpdateQueryParameterCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.OperationId)
            .NotEmpty();

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Value)
            .MaximumLength(500);
    }
}
