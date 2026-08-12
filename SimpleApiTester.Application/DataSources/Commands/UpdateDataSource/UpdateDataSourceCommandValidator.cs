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

            RuleFor(x => x.BaseUrl)
                .NotEmpty()
                .MaximumLength(500)
                .Must(url =>
                    Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                    (result.Scheme == Uri.UriSchemeHttp ||
                     result.Scheme == Uri.UriSchemeHttps))
                .WithMessage("BaseUrl must be a valid HTTP/HTTPS URL.");
        }
    }
}
