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
    public class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
    {
        public void Configure(EntityTypeBuilder<PlatformSettings> builder)
        {
            builder.ToTable("PlatformSettings");

            builder.Property(ps => ps.Key)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ps => ps.Value)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(ps => ps.Description)
                .HasMaxLength(500);

            builder.HasIndex(ps => ps.Key)
                .IsUnique();
        }
    }
}
