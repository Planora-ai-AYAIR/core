using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Parcels;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        builder.ToTable("AnalysisJobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
               .ValueGeneratedNever();

        builder.Property(j => j.ParcelId)
               .IsRequired();

        builder.Property(j => j.PythonJobId)
               .IsRequired()
               .HasMaxLength(256);

        builder.Property(j => j.Type)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(64);

        builder.Property(j => j.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(64);

        builder.Property(j => j.ErrorMessage)
               .HasMaxLength(2048);

        builder.Property(j => j.CompletedAt);

        builder.Property(j => j.CreatedAt)
               .IsRequired();

        builder.Property(j => j.CreatedBy);
        builder.Property(j => j.UpdatedAt);
        builder.Property(j => j.UpdatedBy);

        builder.HasIndex(j => j.PythonJobId)
               .IsUnique();

        builder.HasIndex(j => j.ParcelId);

        builder.HasIndex(j => j.Status);

        builder.HasOne<Parcel>()
               .WithMany()
               .HasForeignKey(j => j.ParcelId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(j => j.Options, optionsBuilder =>
        {
            optionsBuilder.ToTable("AnalysisJobs");
            optionsBuilder.Property(o => o.IncludeTopography)
                          .HasColumnName("options_include_topography")
                          .HasDefaultValue(false);
            optionsBuilder.Property(o => o.IncludeSoil)
                          .HasColumnName("options_include_soil")
                          .HasDefaultValue(false);
            optionsBuilder.Property(o => o.IncludeBearing)
                          .HasColumnName("options_include_bearing")
                          .HasDefaultValue(false);
            optionsBuilder.Property(o => o.IncludeRisk)
                          .HasColumnName("options_include_risk")
                          .HasDefaultValue(false);
            optionsBuilder.Property(o => o.IncludeBorehole)
                          .HasColumnName("options_include_borehole")
                          .HasDefaultValue(false);
            optionsBuilder.Property(o => o.ContourInterval)
                          .HasColumnName("options_contour_interval")
                          .HasPrecision(10, 4);
            optionsBuilder.Property(o => o.SlopeCategories)
                          .HasColumnName("options_slope_categories")
                          .HasColumnType("jsonb");
            optionsBuilder.Property(o => o.ReferencePlane)
                          .HasColumnName("options_reference_plane")
                          .HasMaxLength(64);
            optionsBuilder.Property(o => o.SoilDepths)
                          .HasColumnName("options_soil_depths")
                          .HasColumnType("jsonb");
        });

        builder.Navigation(j => j.Options).IsRequired(false);
    }
}