using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Drill;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrillEntity = Koralytics.Domain.Entities.Drill.Drill;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Drill
{
    public class DrillConfiguration : IEntityTypeConfiguration<DrillEntity>
    {
        public void Configure(EntityTypeBuilder<DrillEntity> builder)
        {
            builder.ToTable("Drills");

            builder.Property(d => d.Mode)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(d => d.DifficultyLevel)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(d => d.Notes)
                .HasMaxLength(1000);

            builder.HasOne(d => d.DrillSession)
                .WithMany(ds => ds.SessionDrills)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.DrillTemplate)
                .WithMany(dt => dt.TemplateDrills)
                .HasForeignKey(d => d.DrillTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.DrillResults)
                .WithOne(dr => dr.Drill)
                .HasForeignKey(dr => dr.DrillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(d => d.SessionId);
            builder.HasIndex(d => d.DrillTemplateId);
        }
    }
}
