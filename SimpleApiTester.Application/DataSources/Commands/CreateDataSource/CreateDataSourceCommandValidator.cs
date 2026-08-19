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

        RuleFor(x => x.DefaultTimeoutSeconds)
            .InclusiveBetween(
                DataSourceTimeoutPolicy.MinTimeoutSeconds,
                DataSourceTimeoutPolicy.MaxTimeoutSeconds)
            .When(x => x.DefaultTimeoutSeconds.HasValue);
    }
}
