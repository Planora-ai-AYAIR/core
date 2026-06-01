using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    Guid UserId,
    string Otp,
    string NewPassword,
    string ConfirmPassword) : IRequest<Response<ResetPasswordResponse>>;
