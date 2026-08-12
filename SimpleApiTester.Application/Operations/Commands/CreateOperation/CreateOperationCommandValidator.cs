using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.Operations.Commands.CreateOperation
{
    public sealed class CreateOperationCommandValidator
    : AbstractValidator<CreateOperationCommand>
    {
        public CreateOperationCommandValidator()
        {
            RuleFor(x => x.DataSourceId)
                .NotEmpty();

            RuleFor(x => x.ApiName)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.Endpoint)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.MethodType)
                .IsInEnum();
        }
    }
}
