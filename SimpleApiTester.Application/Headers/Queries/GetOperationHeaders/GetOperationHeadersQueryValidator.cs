using FluentValidation;

namespace SimpleApiTester.Application.Headers.Queries.GetOperationHeaders;

public sealed class GetOperationHeadersQueryValidator : AbstractValidator<GetOperationHeadersQuery>
{
    public GetOperationHeadersQueryValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty();
    }
}
