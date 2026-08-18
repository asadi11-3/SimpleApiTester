using FluentValidation;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.DeleteDataSourceEnvironment;

public sealed class DeleteDataSourceEnvironmentCommandValidator : AbstractValidator<DeleteDataSourceEnvironmentCommand>
{
    public DeleteDataSourceEnvironmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
