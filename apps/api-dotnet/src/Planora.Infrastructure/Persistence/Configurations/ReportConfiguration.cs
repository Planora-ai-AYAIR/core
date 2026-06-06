using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Parcels;
using Planora.Domain.Payments;
using Planora.Domain.Reports;
using Planora.Infrastructure.Identity;

namespace Planora.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        // 1. Enum Conversions
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Tier).HasConversion<string>().HasMaxLength(20);

        // 2. FK to Parcel (RESTRICT)
        builder.HasOne<Parcel>()
            .WithMany()
            .HasForeignKey(r => r.ParcelId)
            .OnDelete(DeleteBehavior.Restrict);

        // 3. Denormalized FK to User (RESTRICT)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. FK to Payment (RESTRICT)
        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // Nullable for free trial
    }
}