using Microsoft.EntityFrameworkCore;

namespace Planora.Infrastructure.Persistence.Contexts;


public class PlanoraDbContext : DbContext
{
    public PlanoraDbContext(DbContextOptions<PlanoraDbContext> options) : base(options)
    {
    }

    // --- DbSets here as domain entities are created ---

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanoraDbContext).Assembly);
    }
}
