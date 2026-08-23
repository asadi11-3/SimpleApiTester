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
            .Must(value => value is null || Enum.IsDefined(value.Value))
            .WithMessage("ValueSourceType must be a valid enum value.")
            .Must(value => value is null || DataSourceAuthenticationRules.IsAllowedValueSourceType(value.Value))
            .WithMessage("ValueSourceType must be Variable, UserSecret, or EnvironmentVariable.");

        RuleFor(x => x.UsernameSourceType)
            .Must(value => value is null || Enum.IsDefined(value.Value))
            .WithMessage("UsernameSourceType must be a valid enum value.")
            .Must(value => value is null || DataSourceAuthenticationRules.IsAllowedValueSourceType(value.Value))
            .WithMessage("UsernameSourceType must be Variable, UserSecret, or EnvironmentVariable.");

        RuleFor(x => x.PasswordSourceType)
            .Must(value => value is null || Enum.IsDefined(value.Value))
            .WithMessage("PasswordSourceType must be a valid enum value.")
            .Must(value => value is null || DataSourceAuthenticationRules.IsAllowedValueSourceType(value.Value))
            .WithMessage("PasswordSourceType must be Variable, UserSecret, or EnvironmentVariable.");

        RuleFor(x => x.SourceKey)
            .MaximumLength(500)
            .Must(x => x is null || !DataSourceAuthenticationRules.HasCrOrLf(x))
            .WithMessage("SourceKey cannot contain CR or LF characters.");

        RuleFor(x => x.UsernameSourceKey)
            .MaximumLength(500)
            .Must(x => x is null || !DataSourceAuthenticationRules.HasCrOrLf(x))
            .WithMessage("UsernameSourceKey cannot contain CR or LF characters.");

        RuleFor(x => x.PasswordSourceKey)
            .MaximumLength(500)
            .Must(x => x is null || !DataSourceAuthenticationRules.HasCrOrLf(x))
            .WithMessage("PasswordSourceKey cannot contain CR or LF characters.");

        When(
            x => x.AuthenticationType == AuthenticationType.Bearer,
            () =>
            {
                RuleFor(x => x.ValueSourceType)
                    .NotNull()
                    .WithMessage("ValueSourceType is required for Bearer authentication.");

                RuleFor(x => x.SourceKey)
                    .NotEmpty()
                    .WithMessage("SourceKey is required for Bearer authentication.");

                RuleFor(x => x.ApiKeyHeaderName)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage("ApiKeyHeaderName must be null for Bearer authentication.");

                RuleFor(x => x.UsernameSourceType)
                    .Null()
                    .WithMessage("UsernameSourceType must be null for Bearer authentication.");

                RuleFor(x => x.UsernameSourceKey)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage("UsernameSourceKey must be null for Bearer authentication.");

                RuleFor(x => x.PasswordSourceType)
                    .Null()
                    .WithMessage("PasswordSourceType must be null for Bearer authentication.");

                RuleFor(x => x.PasswordSourceKey)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage("PasswordSourceKey must be null for Bearer authentication.");
            });

        When(
            x => x.AuthenticationType == AuthenticationType.ApiKey,
            () =>
            {
                RuleFor(x => x.ValueSourceType)
                    .NotNull()
                    .WithMessage("ValueSourceType is required for API key authentication.");

                RuleFor(x => x.SourceKey)
                    .NotEmpty()
                    .WithMessage("SourceKey is required for API key authentication.");

                RuleFor(x => x.ApiKeyHeaderName)
                    .NotEmpty()
                    .MaximumLength(100)
                    .Must(x => x is not null && !DataSourceAuthenticationRules.HasCrOrLf(x))
                    .WithMessage("ApiKeyHeaderName cannot contain CR or LF characters.")
                    .Must(x => x is not null && !DataSourceAuthenticationRules.IsDisallowedApiKeyHeaderName(x))
                    .WithMessage("ApiKeyHeaderName must not be Authorization or a reserved framework header.");

                RuleFor(x => x.UsernameSourceType)
                    .Null()
                    .WithMessage("UsernameSourceType must be null for API key authentication.");

                RuleFor(x => x.UsernameSourceKey)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage("UsernameSourceKey must be null for API key authentication.");

                RuleFor(x => x.PasswordSourceType)
                    .Null()
                    .WithMessage("PasswordSourceType must be null for API key authentication.");

                RuleFor(x => x.PasswordSourceKey)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage("PasswordSourceKey must be null for API key authentication.");
            });

        When(
            x => x.AuthenticationType == AuthenticationType.Basic,
            () =>
            {
                RuleFor(x => x.UsernameSourceType)
                    .NotNull()
                    .WithMessage("UsernameSourceType is required for Basic authentication.");

                RuleFor(x => x.UsernameSourceKey)
                    .NotEmpty()
                    .WithMessage("UsernameSourceKey is required for Basic authentication.");

                RuleFor(x => x.PasswordSourceType)
                    .NotNull()
                    .WithMessage("PasswordSourceType is required for Basic authentication.");

                RuleFor(x => x.PasswordSourceKey)
                    .NotEmpty()
                    .WithMessage("PasswordSourceKey is required for Basic authentication.");

                RuleFor(x => x.ValueSourceType)
                    .Null()
                    .WithMessage("ValueSourceType must be null for Basic authentication.");

                RuleFor(x => x.SourceKey)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage("SourceKey must be null for Basic authentication.");

                RuleFor(x => x.ApiKeyHeaderName)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage("ApiKeyHeaderName must be null for Basic authentication.");
            });
    }
}
