namespace Planora.Application.Features.Auth.Dtos;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? PhoneNumber,
    string FirstName,
    string LastName);
