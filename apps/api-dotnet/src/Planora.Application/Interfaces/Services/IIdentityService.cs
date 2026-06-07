using Planora.Application.Features.Auth.Dtos;
using Planora.Domain.Shared.Results;

public interface IIdentityService
{
    Task<Result<AuthUserDto>> AuthenticateAsync(string email, string password);
    Task<Result<AuthUserDto>> GetUserByIdAsync(Guid userId);
    Task<bool> IsInRoleAsync(Guid userId, string role);
    Task<bool> AuthorizeAsync(Guid userId, string policyName);
    Task<string?> GetUserNameAsync(Guid userId);

    // if we need a profile for UserInfo
    Task<Result<UserInfo>> GetUserInfoAsync(Guid userId);
}