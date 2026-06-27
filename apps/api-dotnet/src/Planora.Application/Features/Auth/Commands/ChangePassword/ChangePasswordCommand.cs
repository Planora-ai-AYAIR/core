using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : IRequest<Result<ChangePasswordResponse>>;
