using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
