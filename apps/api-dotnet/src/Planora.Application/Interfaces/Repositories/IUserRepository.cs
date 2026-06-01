using Planora.Application.Interfaces.Repositories.DTOs;

namespace Planora.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<UserInfo?> FindByEmailAsync(string email, CancellationToken ct);
    Task<UserInfo?> FindByPhoneAsync(string phoneNumber, CancellationToken ct);
    Task<UserInfo?> FindByIdAsync(Guid userId, CancellationToken ct);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
    Task<(bool Succeeded, string[] Errors, UserInfo? User)> CreateAsync(
        CreateUserRequest request,
        string password,
        CancellationToken ct);
    Task<(bool Succeeded, string[] Errors)> SetEmailConfirmedAsync(
        Guid userId,
        bool confirmed,
        CancellationToken ct);
    Task<(bool Succeeded, string[] Errors)> AddToRoleAsync(Guid userId, string role, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct);
    Task<string?> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct);
    Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(
        Guid userId,
        string resetToken,
        string newPassword,
        CancellationToken ct);
    Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct);
}
