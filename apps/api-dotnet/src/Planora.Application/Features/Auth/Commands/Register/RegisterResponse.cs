namespace Planora.Application.Features.Auth.Commands.Register;

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsEmailConfirmed);
