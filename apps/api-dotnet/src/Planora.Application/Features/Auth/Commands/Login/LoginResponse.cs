namespace Planora.Application.Features.Auth.Commands.Login;

public sealed record LoginResponse(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Role,
    bool IsEmailConfirmed,
    string? AccessToken,
    string? RefreshToken);
