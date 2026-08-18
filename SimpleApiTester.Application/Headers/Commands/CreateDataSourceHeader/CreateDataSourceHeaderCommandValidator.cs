using FluentValidation;

namespace SimpleApiTester.Application.Headers.Commands.CreateDataSourceHeader;

public sealed class CreateDataSourceHeaderCommandValidator : AbstractValidator<CreateDataSourceHeaderCommand>
{
    public CreateDataSourceHeaderCommandValidator()
    {
        Include(new HeaderCommandValidatorBase<CreateDataSourceHeaderCommand>());

        RuleFor(x => x.DataSourceId)
            .NotEmpty();
    }
}
