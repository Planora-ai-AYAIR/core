using FluentValidation;

namespace Planora.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Otp).NotEmpty().Length(4, 8);
    }
}
