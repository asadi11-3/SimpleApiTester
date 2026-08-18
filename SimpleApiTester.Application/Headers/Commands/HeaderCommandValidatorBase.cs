using FluentValidation;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Headers.Commands;

internal sealed class HeaderCommandValidatorBase<TCommand> : AbstractValidator<TCommand>
    where TCommand : class
{
    public HeaderCommandValidatorBase()
    {
        RuleFor(x => GetKey(x))
            .NotEmpty()
            .MaximumLength(100)
            .Must(key => !HeaderRules.IsReservedHeaderKey(key))
            .WithMessage("Header key is reserved and cannot be set explicitly.")
            .Must(key => !HeaderRules.HasCrOrLf(key))
            .WithMessage("Header key cannot contain CR or LF characters.");

        RuleFor(x => GetValue(x))
            .MaximumLength(2000)
            .Must(value => value is null || !HeaderRules.HasCrOrLf(value))
            .WithMessage("General header value cannot contain CR or LF characters.")
            .When(x => GetValueSourceType(x) == HeaderValueSourceType.General);

        RuleFor(x => GetSourceKey(x))
            .MaximumLength(500);

        RuleFor(x => GetValueSourceType(x))
            .IsInEnum();

        RuleFor(x => x)
            .Must(x => HeaderRules.HasValidValueShape(GetValueSourceType(x), GetValue(x), GetSourceKey(x)))
            .WithMessage("Header value and source key do not match the selected value source type.");
    }

    private static string GetKey(TCommand command) => command switch
    {
        CreateDataSourceHeader.CreateDataSourceHeaderCommand createDataSourceHeader => createDataSourceHeader.Key,
        CreateOperationHeader.CreateOperationHeaderCommand createOperationHeader => createOperationHeader.Key,
        UpdateHeader.UpdateHeaderCommand updateHeader => updateHeader.Key,
        _ => throw new InvalidOperationException($"Unsupported command type '{typeof(TCommand).Name}'.")
    };

    private static HeaderValueSourceType GetValueSourceType(TCommand command) => command switch
    {
        CreateDataSourceHeader.CreateDataSourceHeaderCommand createDataSourceHeader => createDataSourceHeader.ValueSourceType,
        CreateOperationHeader.CreateOperationHeaderCommand createOperationHeader => createOperationHeader.ValueSourceType,
        UpdateHeader.UpdateHeaderCommand updateHeader => updateHeader.ValueSourceType,
        _ => throw new InvalidOperationException($"Unsupported command type '{typeof(TCommand).Name}'.")
    };

    private static string? GetValue(TCommand command) => command switch
    {
        CreateDataSourceHeader.CreateDataSourceHeaderCommand createDataSourceHeader => createDataSourceHeader.Value,
        CreateOperationHeader.CreateOperationHeaderCommand createOperationHeader => createOperationHeader.Value,
        UpdateHeader.UpdateHeaderCommand updateHeader => updateHeader.Value,
        _ => throw new InvalidOperationException($"Unsupported command type '{typeof(TCommand).Name}'.")
    };

    private static string? GetSourceKey(TCommand command) => command switch
    {
        CreateDataSourceHeader.CreateDataSourceHeaderCommand createDataSourceHeader => createDataSourceHeader.SourceKey,
        CreateOperationHeader.CreateOperationHeaderCommand createOperationHeader => createOperationHeader.SourceKey,
        UpdateHeader.UpdateHeaderCommand updateHeader => updateHeader.SourceKey,
        _ => throw new InvalidOperationException($"Unsupported command type '{typeof(TCommand).Name}'.")
    };
}
