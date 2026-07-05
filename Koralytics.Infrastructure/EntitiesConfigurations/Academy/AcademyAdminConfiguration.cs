using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Academy;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Academy
{
    public class AcademyAdminConfiguration : IEntityTypeConfiguration<AcademyAdmin>
    {
        public void Configure(EntityTypeBuilder<AcademyAdmin> builder)
        {
            builder.ToTable("AcademyAdmins");

            builder.HasOne(a => a.Academy)
                   .WithOne()
                   .HasForeignKey<AcademyAdmin>(a => a.AcademyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
