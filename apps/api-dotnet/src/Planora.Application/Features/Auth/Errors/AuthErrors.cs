using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Errors;

public static class AuthErrors
{
    public static readonly Error UserNotFound = Error.NotFound("Auth.UserNotFound", "User not found.");
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
    public static readonly Error EmailAlreadyExists = Error.Conflict("Auth.EmailAlreadyExists", "Email already exists.");
    public static readonly Error PhoneAlreadyExists = Error.Conflict("Auth.PhoneAlreadyExists", "Phone number already exists.");
    public static readonly Error EmailAlreadyVerified = Error.Conflict("Auth.EmailAlreadyVerified", "Email is already verified.");
    public static readonly Error InvalidOtp = Error.Validation("Auth.InvalidOtp", "Invalid or expired OTP.");
    public static readonly Error PasswordResetFailed = Error.Failure("Auth.PasswordResetFailed", "Unable to reset password.");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");

    public static Error EmailNotConfirmed(Guid userId) =>
        Error.Forbidden(
            "Auth.EmailNotConfirmed",
            "Email not confirmed. OTP sent to your email.",
            new Dictionary<string, object?>
            {
                ["userId"] = userId
            });
}