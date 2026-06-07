// Planora.Infrastructure/Identity/IdentityService.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Planora.Application.Features.Auth.Dtos;
using Planora.Domain.Shared.Results;

namespace Planora.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<User> userManager,
    IUserClaimsPrincipalFactory<User> claimsPrincipalFactory,
    IAuthorizationService authorizationService) : IIdentityService
{
    public async Task<Result<AuthUserDto>> AuthenticateAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Error.NotFound("Identity.UserNotFound", $"User not found.");

        if (!user.EmailConfirmed)
            return Error.Conflict("Identity.EmailNotConfirmed", "Email not confirmed.");

        if (!await userManager.CheckPasswordAsync(user, password))
            return Error.Unauthorized("Identity.InvalidCredentials", "Invalid email or password.");

        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);

        return new AuthUserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            claims,
            user.EmailConfirmed);
    }

    public async Task<Result<AuthUserDto>> GetUserByIdAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Error.NotFound("Identity.UserNotFound", "User not found.");

        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);

        return new AuthUserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            claims,
            user.EmailConfirmed);
    }

    public async Task<Result<UserInfo>> GetUserInfoAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Error.NotFound("Identity.UserNotFound", "User not found.");

        return new UserInfo(
            user.Id,
            user.Email!,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.CompanyName,
            user.Role,
            user.SubscriptionTier,
            user.EmailConfirmed,
            user.CreatedAt);
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user != null && await userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(Guid userId, string policyName)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        var principal = await claimsPrincipalFactory.CreateAsync(user);
        var result = await authorizationService.AuthorizeAsync(principal, policyName);
        return result.Succeeded;
    }

    public async Task<string?> GetUserNameAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user?.UserName;
    }
}