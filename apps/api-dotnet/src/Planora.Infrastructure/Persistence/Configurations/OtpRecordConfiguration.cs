using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Common;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class OtpRecordConfiguration : IEntityTypeConfiguration<OtpRecord>
{
    public void Configure(EntityTypeBuilder<OtpRecord> builder)
    {
        builder.ToTable("otp_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(16);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.UsedAt);
        builder.HasIndex(x => new { x.UserId, x.Purpose });
    }
}
