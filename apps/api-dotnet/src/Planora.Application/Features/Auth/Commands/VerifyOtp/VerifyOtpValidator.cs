using FluentValidation;
using Planora.Application.Features.Auth.Dtos;

namespace Planora.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Otp).NotEmpty().Length(4, 8);
    }
}
