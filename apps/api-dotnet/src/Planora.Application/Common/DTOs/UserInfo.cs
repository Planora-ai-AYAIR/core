namespace Planora.Application.Interfaces.Repositories.DTOs;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string? PhoneNumber,
    string FirstName,
    string LastName,
    bool EmailConfirmed);
