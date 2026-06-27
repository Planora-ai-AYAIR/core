using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class BearingResultConfiguration : IEntityTypeConfiguration<BearingResult>
{
    public void Configure(EntityTypeBuilder<BearingResult> builder)
    {
        builder.ToTable("BearingResults");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
               .ValueGeneratedNever();

        builder.Property(b => b.AnalysisJobId)
               .IsRequired();

        builder.Property(b => b.BearingCapacityKpa)
               .IsRequired();

        builder.Property(b => b.Classification)
               .HasMaxLength(100);

        builder.Property(b => b.Confidence);

        builder.Property(b => b.Range)
               .HasMaxLength(50);

        builder.Property(b => b.TrafficLight)
               .HasMaxLength(20);

        builder.Property(b => b.RecommendedFoundation)
               .HasMaxLength(500);

        builder.Property(b => b.MaxFloorsWithoutDeepFoundation);

        builder.Property(b => b.FloorCountCategory)
               .HasMaxLength(50);

        builder.Property(b => b.BearingMinKpa);
        builder.Property(b => b.BearingMaxKpa);

        builder.Property(b => b.FeatureImportanceJson)
               .HasColumnType("jsonb");

        builder.Property(b => b.SoilFactorsJson)
               .HasColumnType("jsonb");

        builder.Property(b => b.Disclaimer)
               .HasMaxLength(2000);

        builder.Property(b => b.ModelName)
               .HasMaxLength(100);

        builder.Property(b => b.Framework)
               .HasMaxLength(50);

        builder.Property(b => b.TrainingR2);
        builder.Property(b => b.ShapEnabled);

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.HasOne<AnalysisJob>()
               .WithMany()
               .HasForeignKey(b => b.AnalysisJobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
