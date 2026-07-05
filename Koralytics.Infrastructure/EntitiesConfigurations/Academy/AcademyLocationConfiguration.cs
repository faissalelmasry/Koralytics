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
    public class AcademyLocationConfiguration : IEntityTypeConfiguration<AcademyLocation>
    {
        public void Configure(EntityTypeBuilder<AcademyLocation> builder)
        {
            builder.HasOne(al => al.Academy)
                   .WithMany(a => a.AcademyLocations)
                   .HasForeignKey(al => al.AcademyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
