using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Common;
using Planora.Domain.Notifications;
using Planora.Domain.Parcels;
using Planora.Domain.Payments;
using Planora.Domain.Reports;
using Planora.Infrastructure.Identity;

namespace Planora.Infrastructure.Persistence.Contexts;

public class PlanoraDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public PlanoraDbContext(DbContextOptions<PlanoraDbContext> options) : base(options)
    {
    }

    // ==========================================
    // 1. Auth & Security Entities
    // ==========================================
    // Note: DbSet<User> Users is already provided by IdentityDbContext, so no need to declare it.
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<OtpRecord> OtpRecords { get; set; }
    public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }

    // ==========================================
    // 2. GeoSense AI Business Entities
    // ==========================================
    public DbSet<Parcel> Parcels { get; set; }
    public DbSet<AnalysisJob> AnalysisJobs { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportModule> ReportModules { get; set; }
    public DbSet<ReportFile> ReportFiles { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        // Must call base first to configure Identity tables (AspNetUsers, etc.)
        base.OnModelCreating(modelBuilder);

        // Apply all Fluent API configurations (IEntityTypeConfiguration<T>) from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanoraDbContext).Assembly);
    }
}