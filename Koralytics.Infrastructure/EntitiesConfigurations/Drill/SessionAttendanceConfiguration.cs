using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Drill;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Drill
{
    public class SessionAttendanceConfiguration : IEntityTypeConfiguration<SessionAttendance>
    {
        public void Configure(EntityTypeBuilder<SessionAttendance> builder)
        {
            builder.ToTable("SessionAttendances");

            builder.Property(sa => sa.IsPresent)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(sa => sa.DrillSession)
                .WithMany(ds => ds.SessionAttendances)
                .HasForeignKey(sa => sa.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sa => sa.Player)
                .WithMany()
                .HasForeignKey(sa => sa.playerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(sa => sa.SessionId);
            builder.HasIndex(sa => sa.playerId);
            builder.HasIndex(sa => new { sa.SessionId, sa.playerId })
                .IsUnique();
        }
    }
}
