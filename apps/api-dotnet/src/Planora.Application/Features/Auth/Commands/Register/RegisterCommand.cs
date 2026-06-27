using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? PhoneNumber,
    string FirstName,
    string LastName) : IRequest<Result<RegisterResponse>>;
