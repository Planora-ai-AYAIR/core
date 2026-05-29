using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Repositories.DTOs;
using Planora.Infrastructure.Identity;

namespace Planora.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly UserManager<User> _userManager;

    public UserRepository(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserInfo?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<UserInfo?> FindByPhoneAsync(string phoneNumber, CancellationToken ct)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
        return user is null ? null : Map(user);
    }

    public async Task<UserInfo?> FindByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : Map(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is not null && await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<(bool Succeeded, string[] Errors, UserInfo? User)> CreateAsync(
        CreateUserRequest request,
        string password,
        CancellationToken ct)
    {
        var user = new User
        {
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray(), null);
        }

        return (true, Array.Empty<string>(), Map(user));
    }

    public async Task<(bool Succeeded, string[] Errors)> SetEmailConfirmedAsync(
        Guid userId,
        bool confirmed,
        CancellationToken ct)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return (false, new[] { "User not found." });
        }

        user.EmailConfirmed = confirmed;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Succeeded, string[] Errors)> AddToRoleAsync(Guid userId, string role, CancellationToken ct)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return (false, new[] { "User not found." });
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToArray();
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return null;
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(
        Guid userId,
        string resetToken,
        string newPassword,
        CancellationToken ct)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return (false, new[] { "User not found." });
        }

        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return (false, new[] { "User not found." });
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description).ToArray());
    }

    private static UserInfo Map(User user)
    {
        return new UserInfo(
            user.Id,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed);
    }
}
