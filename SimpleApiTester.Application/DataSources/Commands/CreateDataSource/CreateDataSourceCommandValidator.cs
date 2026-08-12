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

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .MaximumLength(500)
            .Must(BeValidUrl)
            .WithMessage("BaseUrl must be a valid absolute URL.");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(
            url,
            UriKind.Absolute,
            out var result)
            &&
            (result.Scheme == Uri.UriSchemeHttp ||
             result.Scheme == Uri.UriSchemeHttps);
    }
}