using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.SystemAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.SystemAdmin
{
    public class PlatformAuditLogConfiguration : IEntityTypeConfiguration<PlatformAuditLog>
    {
        public void Configure(EntityTypeBuilder<PlatformAuditLog> builder)
        {
            builder.ToTable("PlatformAuditLogs");

            builder.Property(pal => pal.Action)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(pal => pal.TargetEntity)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(pal => pal.TargetEntityId)
                .IsRequired();

            builder.Property(pal => pal.Details)
                .HasMaxLength(2000);

            builder.HasIndex(pal => pal.Action);
            builder.HasIndex(pal => pal.TargetEntity);
            builder.HasIndex(pal => pal.TargetEntityId);
            builder.HasIndex(pal => new { pal.TargetEntity, pal.TargetEntityId });

        }
    }
}
