using MediatR;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.Logout;

public sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository) : IRequestHandler<LogoutCommand, Response<LogoutResponse>>
{
    public async Task<Response<LogoutResponse>> Handle(LogoutCommand request, CancellationToken ct)
    {
        await refreshTokenRepository.InvalidateAsync(request.UserId, ct);
        await auditLogRepository.LogAsync(request.UserId, "Logout", null, ct);

        return new ResponseHandler().Success(new LogoutResponse(request.UserId), "Logged out.");
    }
}
