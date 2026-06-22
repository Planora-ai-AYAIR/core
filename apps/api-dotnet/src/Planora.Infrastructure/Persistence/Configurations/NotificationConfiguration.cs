using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Notifications;

namespace Planora.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Message).HasMaxLength(2048).IsRequired();
            builder.Property(x => x.IsRead).IsRequired();
            builder.Property(x => x.ReadAt);
            builder.Property(x => x.Data).HasColumnType("jsonb");

            builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
                .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt")
                .IsDescending(false, false, true);
        }
    }
}
