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
    }
}