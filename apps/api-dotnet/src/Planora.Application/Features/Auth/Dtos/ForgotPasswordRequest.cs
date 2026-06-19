namespace Planora.Application.Features.Auth.Dtos;

public sealed record ForgotPasswordRequest(string? Email, string? PhoneNumber);
