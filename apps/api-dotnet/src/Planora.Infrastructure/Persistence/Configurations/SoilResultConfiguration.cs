using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class SoilResultConfiguration : IEntityTypeConfiguration<SoilResult>
{
    public void Configure(EntityTypeBuilder<SoilResult> builder)
    {
        builder.ToTable("SoilResults");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
               .ValueGeneratedNever();

        builder.Property(s => s.AnalysisJobId)
               .IsRequired();

        builder.Property(s => s.SandPercent)
               .IsRequired();

        builder.Property(s => s.SiltPercent)
               .IsRequired();

        builder.Property(s => s.ClayPercent)
               .IsRequired();

        builder.Property(s => s.BulkDensity)
               .IsRequired();

        builder.Property(s => s.OrganicCarbon)
               .IsRequired();

        builder.Property(s => s.Ph)
               .IsRequired();

        builder.Property(s => s.BearingCapacityEstimate)
               .IsRequired();

        builder.Property(s => s.BearingCapacityCategory)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(s => s.CompositionUnit)
               .HasMaxLength(10);

        builder.Property(s => s.BulkDensityUnit)
               .HasMaxLength(20);

        builder.Property(s => s.OrganicCarbonUnit)
               .HasMaxLength(20);

        builder.Property(s => s.PrimaryType)
               .HasMaxLength(100);

        builder.Property(s => s.UsdaClass)
               .HasMaxLength(100);

        builder.Property(s => s.AiConfidence);

        builder.Property(s => s.MultiDepthProfileJson)
               .HasColumnType("jsonb");

        builder.Property(s => s.HeatmapTileUrl)
               .HasMaxLength(2000);

        builder.Property(s => s.CreatedAt)
               .IsRequired();

        builder.HasOne<AnalysisJob>()
               .WithMany()
               .HasForeignKey(s => s.AnalysisJobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
