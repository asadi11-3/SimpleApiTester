using FluentValidation;

namespace SimpleApiTester.Application.QueryParameters.Queries.GetQueryParametersByOperation;

public sealed class GetQueryParametersByOperationQueryValidator
    : AbstractValidator<GetQueryParametersByOperationQuery>
{
    public GetQueryParametersByOperationQueryValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty();
    }
}
