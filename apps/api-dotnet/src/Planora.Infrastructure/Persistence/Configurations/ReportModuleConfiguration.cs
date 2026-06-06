using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Reports;

namespace Planora.Infrastructure.Persistence.Configurations;

public class ReportModuleConfiguration : IEntityTypeConfiguration<ReportModule>
{
    public void Configure(EntityTypeBuilder<ReportModule> builder)
    {
        builder.ToTable("ReportModules");

        // 1. Enums 
        builder.Property(m => m.ModuleType).HasConversion<string>().HasMaxLength(30);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        // 2. JSONB Column Mapping
        builder.Property(m => m.OutputMetadata)
            .HasColumnType("jsonb");

        // 3. Unique Constraint (One module type per report)
        builder.HasIndex(m => new { m.ReportId, m.ModuleType })
            .IsUnique();

        // 4. JSONB Index (GIN)
        builder.HasIndex(m => m.OutputMetadata).HasMethod("GIN");

        // 5. FK to Report (RESTRICT)
        builder.HasOne<Report>()
            .WithMany(r => r.Modules) // Assuming navigation property exists
            .HasForeignKey(m => m.ReportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}