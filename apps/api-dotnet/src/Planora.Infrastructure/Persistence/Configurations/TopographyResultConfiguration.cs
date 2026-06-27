using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class TopographyResultConfiguration : IEntityTypeConfiguration<TopographyResult>
{
    public void Configure(EntityTypeBuilder<TopographyResult> builder)
    {
        builder.ToTable("TopographyResults");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
               .ValueGeneratedNever();

        builder.Property(t => t.AnalysisJobId)
               .IsRequired();

        builder.Property(t => t.ElevationMin)
               .IsRequired();

        builder.Property(t => t.ElevationMax)
               .IsRequired();

        builder.Property(t => t.ElevationMean)
               .IsRequired();

        builder.Property(t => t.SlopeDistributionJson)
               .HasColumnType("jsonb");

        builder.Property(t => t.CutVolume)
               .IsRequired();

        builder.Property(t => t.FillVolume)
               .IsRequired();

        builder.Property(t => t.NetVolume)
               .IsRequired();

        builder.Property(t => t.ContourInterval)
               .IsRequired();

        builder.Property(t => t.ContourGeoJsonUrl)
               .HasMaxLength(2000);

        builder.Property(t => t.PondingGeoJsonUrl)
               .HasMaxLength(2000);

        builder.Property(t => t.PondingZonesCount);

        builder.Property(t => t.PondingTotalArea);

        builder.Property(t => t.ElevationTileUrl)
               .HasMaxLength(2000);

        builder.Property(t => t.SlopeTileUrl)
               .HasMaxLength(2000);

        builder.Property(t => t.DemRasterUrl)
               .HasMaxLength(2000);

        builder.Property(t => t.SlopeRasterUrl)
               .HasMaxLength(2000);

        builder.Property(t => t.CopernicusDemVersion)
               .HasMaxLength(50);

        builder.Property(t => t.PixelResolutionMeters);

        builder.Property(t => t.Crs)
               .HasMaxLength(20);

        builder.Property(t => t.ProcessingTimeSeconds);

        builder.Property(t => t.CreatedAt)
               .IsRequired();

        builder.HasOne<AnalysisJob>()
               .WithMany()
               .HasForeignKey(t => t.AnalysisJobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
