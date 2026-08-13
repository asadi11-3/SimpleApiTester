using FluentValidation;

namespace SimpleApiTester.Application.QueryParameters.Commands.DeleteQueryParameter;

public sealed class DeleteQueryParameterCommandValidator
    : AbstractValidator<DeleteQueryParameterCommand>
{
    public DeleteQueryParameterCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
