using FluentValidation;
using Planora.Application.Features.Auth.Dtos;

namespace Planora.Application.Features.Auth.Commands.Login;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
