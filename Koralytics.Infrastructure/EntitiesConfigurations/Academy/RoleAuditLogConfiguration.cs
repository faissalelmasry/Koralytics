using Koralytics.Domain.Entities.Academy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Academy
{
    public class RoleAuditLogConfiguration : IEntityTypeConfiguration<RoleAuditLog>
    {
        public void Configure(EntityTypeBuilder<RoleAuditLog> builder)
        {
            builder.HasOne(r => r.Academy)
                   .WithMany(a => a.RoleAuditLogs)
                   .HasForeignKey(r => r.AcademyId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.PerformedByUser)
                   .WithMany()
                   .HasForeignKey(r => r.PerformedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.AffectedUser)
                   .WithMany()
                   .HasForeignKey(r => r.AffectedUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.AcademyId);
            builder.HasIndex(x => x.PerformedByUserId);
        }
    }
}
