using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Entities;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class BoreholeResultConfiguration : IEntityTypeConfiguration<BoreholeResult>
{
    public void Configure(EntityTypeBuilder<BoreholeResult> builder)
    {
        builder.ToTable("BoreholeResults");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
               .ValueGeneratedNever();

        builder.Property(b => b.AnalysisJobId)
               .IsRequired();

        builder.Property(b => b.MinimumRequired)
               .IsRequired();

        builder.Property(b => b.OptimalCount)
               .IsRequired();

        builder.Property(b => b.CoveragePercentage)
               .IsRequired();

        builder.Property(b => b.GridSize)
               .HasMaxLength(50);

        builder.Property(b => b.PlacementStrategy)
               .HasMaxLength(200);

        builder.Property(b => b.PlacementPointsJson)
               .HasColumnType("jsonb");

        builder.Property(b => b.PlacementGeoJsonUrl)
               .HasMaxLength(2000);

        builder.Property(b => b.TraditionalBoreholeCount)
               .IsRequired();

        builder.Property(b => b.TraditionalEstimatedCost)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(b => b.OptimizedBoreholeCount)
               .IsRequired();

        builder.Property(b => b.OptimizedEstimatedCost)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(b => b.SavingsAmount)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(b => b.SavingsPercentage)
               .IsRequired();

        builder.Property(b => b.Currency)
               .HasMaxLength(10);

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.HasOne<AnalysisJob>()
               .WithMany()
               .HasForeignKey(b => b.AnalysisJobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
