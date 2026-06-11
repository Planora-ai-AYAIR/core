namespace Planora.Application.Features.Auth.Dtos;

public sealed record VerifyOtpRequest(Guid UserId, string Otp);
