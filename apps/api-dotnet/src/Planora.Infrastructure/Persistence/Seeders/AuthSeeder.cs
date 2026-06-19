using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Auth;
using Planora.Infrastructure.Identity;
using Planora.Infrastructure.Options;

namespace Planora.Infrastructure.Persistence.Seeders;

public static class AuthSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("AuthSeeder");
        var config = provider.GetRequiredService<IConfiguration>();
        var userManager = provider.GetRequiredService<UserManager<User>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var options = config.GetSection(SeedUsersOptions.SectionName).Get<SeedUsersOptions>()
            ?? new SeedUsersOptions();

        await EnsureRoleAsync(roleManager, AuthRoles.Admin, ct);
        await EnsureRoleAsync(roleManager, AuthRoles.Client, ct);

        await EnsureUserAsync(userManager, roleManager, options.Admin, AuthRoles.Admin, logger, ct);
        await EnsureUserAsync(userManager, roleManager, options.Client, AuthRoles.Client, logger, ct);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        string role,
        CancellationToken ct)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        SeedUserOptions options,
        string role,
        ILogger? logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger?.LogWarning("Seed user for role {Role} is missing email or password.", role);
            return;
        }

        var user = await userManager.FindByEmailAsync(options.Email);
        if (user is null)
        {
            user = new User
            {
                Email = options.Email,
                UserName = options.Email,
                PhoneNumber = options.PhoneNumber,
                FirstName = options.FirstName,
                LastName = options.LastName,
                EmailConfirmed = options.EmailConfirmed
            };

            var createResult = await userManager.CreateAsync(user, options.Password);
            if (!createResult.Succeeded)
            {
                logger?.LogError("Failed to create seed user {Email}: {Errors}",
                    options.Email,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                logger?.LogError("Failed to assign role {Role} to {Email}: {Errors}",
                    role,
                    options.Email,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
