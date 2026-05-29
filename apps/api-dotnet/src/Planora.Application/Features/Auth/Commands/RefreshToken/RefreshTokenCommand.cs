using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Response<RefreshTokenResponse>>;
