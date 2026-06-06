using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(Guid UserId) : IRequest<Result<LogoutResponse>>;
