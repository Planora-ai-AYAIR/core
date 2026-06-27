using Microsoft.AspNetCore.Identity;
using Planora.Application.Interfaces.Repositories;

namespace Planora.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RoleRepository(RoleManager<IdentityRole<Guid>> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task EnsureRoleExistsAsync(string role, CancellationToken ct)
    {
        if (await _roleManager.RoleExistsAsync(role))
        {
            return;
        }

        await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }
}
