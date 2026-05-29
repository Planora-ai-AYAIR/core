namespace Planora.Application.Features.Auth.Commands.ResendOtp;

public sealed record ResendOtpResponse(Guid UserId, bool EmailConfirmed);
