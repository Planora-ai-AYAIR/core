using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class RiskResultConfiguration : IEntityTypeConfiguration<RiskResult>
{
    public void Configure(EntityTypeBuilder<RiskResult> builder)
    {
        builder.ToTable("RiskResults");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .ValueGeneratedNever();

        builder.Property(r => r.AnalysisJobId)
               .IsRequired();

        builder.Property(r => r.FloodRiskScore)
               .IsRequired();

        builder.Property(r => r.SeismicRiskScore)
               .IsRequired();

        builder.Property(r => r.ExpansiveSoilRisk)
               .IsRequired();

        builder.Property(r => r.LiquefactionRisk)
               .IsRequired();

        builder.Property(r => r.OverallRiskScore)
               .IsRequired();

        builder.Property(r => r.OverallRiskLevel)
               .HasMaxLength(20);

        builder.Property(r => r.FloodLevel)
               .HasMaxLength(20);

        builder.Property(r => r.FloodFactorsJson)
               .HasColumnType("jsonb");

        builder.Property(r => r.FloodGeoJsonUrl)
               .HasMaxLength(2000);

        builder.Property(r => r.SeismicLevel)
               .HasMaxLength(20);

        builder.Property(r => r.SeismicFactorsJson)
               .HasColumnType("jsonb");

        builder.Property(r => r.SeismicSource)
               .HasMaxLength(200);

        builder.Property(r => r.SeismicZone)
               .HasMaxLength(50);

        builder.Property(r => r.ExpansiveSoilLevel)
               .HasMaxLength(20);

        builder.Property(r => r.ExpansiveSoilFactorsJson)
               .HasColumnType("jsonb");

        builder.Property(r => r.ReplacementDepth);

        builder.Property(r => r.LiquefactionLevel)
               .HasMaxLength(20);

        builder.Property(r => r.LiquefactionFactorsJson)
               .HasColumnType("jsonb");

        builder.Property(r => r.LiquefactionSusceptibility)
               .HasMaxLength(20);

        builder.Property(r => r.LiquefactionMethodology)
               .HasMaxLength(200);

        builder.Property(r => r.RiskHeatmapTileUrl)
               .HasMaxLength(2000);

        builder.Property(r => r.MitigationSuggestionsJson)
               .HasColumnType("jsonb");

        builder.Property(r => r.CreatedAt)
               .IsRequired();

        builder.HasOne<AnalysisJob>()
               .WithMany()
               .HasForeignKey(r => r.AnalysisJobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
