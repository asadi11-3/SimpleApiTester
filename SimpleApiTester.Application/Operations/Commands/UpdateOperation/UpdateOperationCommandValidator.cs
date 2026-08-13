using FluentValidation;

namespace SimpleApiTester.Application.Operations.Commands.UpdateOperation;

public sealed class UpdateOperationCommandValidator
    : AbstractValidator<UpdateOperationCommand>
{
    public UpdateOperationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.DataSourceId)
            .NotEmpty();

        RuleFor(x => x.ApiName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Endpoint)
            .NotEmpty()
            .MaximumLength(500)
            .Must(BeValidEndpoint)
            .WithMessage("Endpoint must be a relative path without a query string.");

        RuleFor(x => x.MethodType)
            .IsInEnum();

        RuleFor(x => x.ContentType)
            .Must(ContentTypeRules.IsValid)
            .WithMessage("ContentType must be a valid media type.");
    }

    private static bool BeValidEndpoint(string endpoint)
    {
        var trimmedEndpoint = endpoint.Trim();

        if (string.IsNullOrWhiteSpace(trimmedEndpoint) || trimmedEndpoint.StartsWith("//"))
        {
            return false;
        }

        if (trimmedEndpoint.Contains('?'))
        {
            return false;
        }

        return !Uri.TryCreate(trimmedEndpoint, UriKind.Absolute, out _);
    }
}
