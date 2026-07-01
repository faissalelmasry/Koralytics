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
    public class DrillTemplateConfiguration : IEntityTypeConfiguration<DrillTemplate>
    {
        public void Configure(EntityTypeBuilder<DrillTemplate> builder)
        {
            builder.ToTable("DrillTemplates");

            builder.Property(dt => dt.Name)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(dt => dt.DifficultyLevel)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(dt => dt.DrillMode)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(dt => dt.IsShared)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(dt => dt.DrillCategory)
                .WithMany(dc => dc.DrillTemplates)
                .HasForeignKey(dt => dt.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(dt => dt.DrillTemplateAcademy)
                .WithMany()
                .HasForeignKey(dt => dt.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(dt => dt.TemplateDrills)
                .WithOne(d => d.DrillTemplate)
                .HasForeignKey(d => d.DrillTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(dt => dt.CategoryId);
            builder.HasIndex(dt => dt.AcademyId);
            builder.HasIndex(dt => dt.IsShared);
            builder.HasIndex(dt => new { dt.AcademyId, dt.IsShared });
        }
    }
}
