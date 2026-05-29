using FluentValidation;

namespace Planora.Application.Features.Auth.Commands.ResendOtp;

public sealed class ResendOtpValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
