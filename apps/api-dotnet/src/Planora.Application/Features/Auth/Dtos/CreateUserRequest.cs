namespace Planora.Application.Interfaces.Repositories.DTOs;

public sealed record CreateUserRequest(
    string Email,
    string? PhoneNumber,
    string FirstName,
    string LastName);
