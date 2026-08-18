using FluentValidation;

namespace SimpleApiTester.Application.DataSources.Commands.CreateDataSource;

public sealed class CreateDataSourceCommandValidator
    : AbstractValidator<CreateDataSourceCommand>
{
    public CreateDataSourceCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100);
    }
}
