using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Commands.UpdateDataSource
{
    public sealed class UpdateDataSourceCommandValidator
    : AbstractValidator<UpdateDataSourceCommand>
    {
        public UpdateDataSourceCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
