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
    public class AcademyBadgeConfiguration : IEntityTypeConfiguration<AcademyBadge>
    {
        public void Configure(EntityTypeBuilder<AcademyBadge> builder)
        {
            builder.HasOne(ab => ab.Academy)
                   .WithMany(a => a.AcademyBadges)
                   .HasForeignKey(ab => ab.AcademyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
