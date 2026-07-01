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
    public class DrillResultConfiguration : IEntityTypeConfiguration<DrillResult>
    {
        public void Configure(EntityTypeBuilder<DrillResult> builder)
        {
            builder.ToTable("DrillResults");

            builder.Property(dr => dr.ManualScore)
                .HasPrecision(5, 2);

            builder.Property(dr => dr.DoneCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(dr => dr.MissedCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(dr => dr.FinalScore)
                .IsRequired()
                .HasPrecision(5, 2);

            builder.Property(dr => dr.CoachNotes)
                .HasMaxLength(1000);

            builder.HasOne(dr => dr.Drill)
                .WithMany(d => d.DrillResults)
                .HasForeignKey(dr => dr.DrillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(dr => dr.Player)
                .WithMany()
                .HasForeignKey(dr => dr.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(dr => dr.DrillId);
            builder.HasIndex(dr => dr.PlayerId);
            builder.HasIndex(dr => new { dr.DrillId, dr.PlayerId })
                .IsUnique();
        }
    }
}
