using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Parcels;
using Planora.Infrastructure.Identity;

namespace Planora.Infrastructure.Persistence.Configurations;

public class ParcelConfiguration : IEntityTypeConfiguration<Parcel>
{
    public void Configure(EntityTypeBuilder<Parcel> builder)
    {
        builder.ToTable("Parcels");

        // 1. Soft Delete Filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        // 2. Spatial Data Types (PostGIS) 
        builder.Property(p => p.Boundary)
            .HasColumnType("geometry")
            .IsRequired();

        builder.Property(p => p.Centroid)
            .HasColumnType("geometry")
            .IsRequired();

        // 3. Enum Conversion
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // 4. FK Relationship with Restrict Delete
        builder.HasOne<User>() // User entity is in Identity, so we navigate blindly by UserId
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 5. Spatial Indexes
        builder.HasIndex(p => p.Boundary).HasMethod("GIST");
        builder.HasIndex(p => p.Centroid).HasMethod("GIST");
    }
}