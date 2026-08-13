using FluentValidation;

namespace SimpleApiTester.Application.QueryParameters.Commands.CreateQueryParameter;

public sealed class CreateQueryParameterCommandValidator
    : AbstractValidator<CreateQueryParameterCommand>
{
    public CreateQueryParameterCommandValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty();

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Value)
            .MaximumLength(500);
    }
}
