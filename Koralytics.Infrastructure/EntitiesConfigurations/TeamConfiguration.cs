using Koralytics.Domain.Entities.Academy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.HasOne<Academy>()
                .WithMany()
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<AgeGroup>()
                .WithMany()
                .HasForeignKey(x => x.AgeGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AcademyLocation>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
        }
    }
}
