using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Match;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Match
{
    public class MatchLineupConfiguration : IEntityTypeConfiguration<MatchLineup>
    {
        public void Configure(EntityTypeBuilder<MatchLineup> builder)
        {
            builder.Property(x => x.IsStarting)
            .IsRequired();

            builder.HasOne(x => x.Match)
                .WithMany(x => x.MatchLineups)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Player)
                .WithMany()
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.IsHomeSide)
                .IsRequired(false);

            builder.HasIndex(ml => new { ml.MatchId, ml.PlayerId, ml.TeamId })
                .IsUnique()
                .HasFilter("[IsHomeSide] IS NULL");

            builder.HasIndex(ml => new { ml.MatchId, ml.PlayerId, ml.TeamId, ml.IsHomeSide })
               .IsUnique()
               .HasFilter("[IsHomeSide] IS NOT NULL");

            builder.HasIndex(ml => new { ml.MatchId, ml.IsStarting });

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_MatchLineup_JerseyNumber",
                    "[JerseyNumber] IS NULL OR ([JerseyNumber] BETWEEN 1 AND 99)");
            });
        }
    }
}
