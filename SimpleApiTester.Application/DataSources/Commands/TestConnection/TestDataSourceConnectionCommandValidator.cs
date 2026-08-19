using FluentValidation;

namespace SimpleApiTester.Application.DataSources.Commands.TestConnection;

public sealed class TestDataSourceConnectionCommandValidator : AbstractValidator<TestDataSourceConnectionCommand>
{
    public TestDataSourceConnectionCommandValidator()
    {
        RuleFor(x => x.DataSourceId)
            .NotEmpty();

        RuleFor(x => x.EnvironmentId)
            .NotEmpty();
    }
}
