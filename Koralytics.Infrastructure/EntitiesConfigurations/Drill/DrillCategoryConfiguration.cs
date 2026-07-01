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
    public class DrillCategoryConfiguration : IEntityTypeConfiguration<DrillCategory>
    {
        public void Configure(EntityTypeBuilder<DrillCategory> builder)
        {
            builder.ToTable("DrillCategories");

            builder.Property(dc => dc.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasMany(dc => dc.DrillTemplates)
                .WithOne(dt => dt.DrillCategory)
                .HasForeignKey(dt => dt.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(dc => dc.Name)
                .IsUnique();
        }
    }
}
