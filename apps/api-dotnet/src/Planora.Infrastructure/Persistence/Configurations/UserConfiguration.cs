using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Infrastructure.Identity;

namespace Planora.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // 1. Global Query Filter for Soft Delete
        builder.HasQueryFilter(u => !u.IsDeleted);

        // 2. Enum Conversion
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        // 3. Properties from Schema
        builder.Property(u => u.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.SubscriptionTier).HasConversion<string>().HasMaxLength(20);
    }
}