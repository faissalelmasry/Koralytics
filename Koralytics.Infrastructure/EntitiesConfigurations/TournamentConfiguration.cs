using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
    {
        public void Configure(EntityTypeBuilder<Tournament> builder)
        {
            builder.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.HasOne(x => x.AgeGroup)
                .WithMany()
                .HasForeignKey(x => x.AgeGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Name).IsUnique();

            builder.HasIndex(x => x.AgeGroupId);

            builder.HasIndex(x => x.Status);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Tournament_Dates",
                    "[EndDate] >= [StartDate]");
            });
        }
    }
}
