using Koralytics.Domain.Entities.SystemAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class AcademyRequestConfiguration : IEntityTypeConfiguration<AcademyRequest>
    {
        public void Configure(EntityTypeBuilder<AcademyRequest> builder)
        {
            builder.Property(x => x.AcademyName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(100);

            builder.HasIndex(x => x.RequestStatus);
        }
    }
}
