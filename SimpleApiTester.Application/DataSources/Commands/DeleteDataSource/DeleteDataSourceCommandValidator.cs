using FluentValidation;

namespace SimpleApiTester.Application.DataSources.Commands.DeleteDataSource;

public sealed class DeleteDataSourceCommandValidator : AbstractValidator<DeleteDataSourceCommand>
{
    public DeleteDataSourceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
