using FluentValidation;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.DataSourceAuthentications.Commands.UpsertDataSourceAuthentication;

public sealed class UpsertDataSourceAuthenticationCommandValidator
    : AbstractValidator<UpsertDataSourceAuthenticationCommand>
{
    public UpsertDataSourceAuthenticationCommandValidator()
    {
        RuleFor(x => x.DataSourceId)
            .NotEmpty();

        RuleFor(x => x.AuthenticationType)
            .IsInEnum();

        RuleFor(x => x.ValueSourceType)
            .IsInEnum()
            .Must(DataSourceAuthenticationRules.IsAllowedValueSourceType)
            .WithMessage("ValueSourceType must be Variable, UserSecret, or EnvironmentVariable.");

        RuleFor(x => x.SourceKey)
            .NotEmpty()
            .MaximumLength(500)
            .Must(x => !DataSourceAuthenticationRules.HasCrOrLf(x))
            .WithMessage("SourceKey cannot contain CR or LF characters.");

        When(
            x => x.AuthenticationType == AuthenticationType.Bearer,
            () => RuleFor(x => x.ApiKeyHeaderName)
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage("ApiKeyHeaderName must be null for Bearer authentication."));

        When(
            x => x.AuthenticationType == AuthenticationType.ApiKey,
            () =>
            {
                RuleFor(x => x.ApiKeyHeaderName)
                    .NotEmpty()
                    .MaximumLength(100)
                    .Must(x => x is not null && !DataSourceAuthenticationRules.HasCrOrLf(x))
                    .WithMessage("ApiKeyHeaderName cannot contain CR or LF characters.")
                    .Must(x => x is not null && !DataSourceAuthenticationRules.IsDisallowedApiKeyHeaderName(x))
                    .WithMessage("ApiKeyHeaderName must not be Authorization or a reserved framework header.");
            });
    }
}
