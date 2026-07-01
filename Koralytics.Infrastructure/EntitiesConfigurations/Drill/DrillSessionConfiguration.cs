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
    public class DrillSessionConfiguration : IEntityTypeConfiguration<DrillSession>
    {
        public void Configure(EntityTypeBuilder<DrillSession> builder)
        {
            builder.ToTable("DrillSessions");

            builder.Property(ds => ds.SessionDate)
                .IsRequired();

            builder.Property(ds => ds.Type)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(ds => ds.Notes)
                .HasMaxLength(1000);

            builder.HasOne(ds => ds.DrillSessionAcademy)
                .WithMany()
                .HasForeignKey(ds => ds.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ds => ds.DrillSessionTeam)
                .WithMany()
                .HasForeignKey(ds => ds.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ds => ds.DrillSessionCoach)
                .WithMany()
                .HasForeignKey(ds => ds.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ds => ds.SessionDrills)
                .WithOne(d => d.DrillSession)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(ds => ds.SessionAttendances)
                .WithOne(sa => sa.DrillSession)
                .HasForeignKey(sa => sa.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(ds => ds.AcademyId);
            builder.HasIndex(ds => ds.TeamId);
            builder.HasIndex(ds => ds.CoachId);
            builder.HasIndex(ds => ds.SessionDate);
            builder.HasIndex(ds => ds.Type);
            builder.HasIndex(ds => new { ds.AcademyId, ds.SessionDate });
            builder.HasIndex(ds => new { ds.TeamId, ds.SessionDate });
        }
    }
}
