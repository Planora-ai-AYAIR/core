using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Planora.Infrastructure.Identity;

namespace Planora.Infrastructure.Persistence.Contexts;


public class PlanoraDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public PlanoraDbContext(DbContextOptions<PlanoraDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanoraDbContext).Assembly);
    }
}
