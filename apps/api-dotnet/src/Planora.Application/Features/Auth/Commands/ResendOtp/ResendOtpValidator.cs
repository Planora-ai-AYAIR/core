using FluentValidation;
using Planora.Application.Features.Auth.Dtos;

namespace Planora.Application.Features.Auth.Commands.ResendOtp;

public sealed class ResendOtpValidator : AbstractValidator<ResendOtpRequest>
{
    public ResendOtpValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
