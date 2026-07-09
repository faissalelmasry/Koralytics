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
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {

            builder.HasOne(t => t.AgeGroup)
                   .WithMany(ag => ag.Teams)
                   .HasForeignKey(t => t.AgeGroupId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c=>c.Coach)
                   .WithMany()
                   .HasForeignKey(t => t.CoachId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Location)
                   .WithMany()
                   .HasForeignKey(t => t.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.AgeGroupId, x.Name }).IsUnique();
        }
    }
}
