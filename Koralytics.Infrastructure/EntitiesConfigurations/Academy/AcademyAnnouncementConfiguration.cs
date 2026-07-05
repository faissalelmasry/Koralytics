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
    public class AcademyAnnouncementConfiguration : IEntityTypeConfiguration<AcademyAnnouncement>
    {
        public void Configure(EntityTypeBuilder<AcademyAnnouncement> builder)
        {
            builder.HasOne(a => a.Academy)
                   .WithMany(ac => ac.AcademyAnnouncements)
                   .HasForeignKey(a => a.AcademyId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.AcademyId);
        }
    }

 }
