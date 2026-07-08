using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Tournment
{
    public class TournamentFixtureConfiguration : IEntityTypeConfiguration<TournamentFixture>
    {
        public void Configure(EntityTypeBuilder<TournamentFixture> entity)
        {

            entity.ToTable("TournamentFixtures", t => t.HasCheckConstraint(
            "CK_TournamentFixture_RoundOrGroup",
            "([RoundId] IS NOT NULL AND [GroupId] IS NULL) OR ([GroupId] IS NOT NULL AND [RoundId] IS NULL)"
            ));

            entity.HasKey(tf => tf.Id);

            entity.Property(tf => tf.LegNumber)
                   .IsRequired()
                   .HasDefaultValue(1);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_TournamentFixture_LegNumber",
                "[LegNumber] IN (1, 2)"
            ));

            entity.HasOne(x => x.Group)
                  .WithMany()
                  .HasForeignKey(x => x.GroupId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Round)
                  .WithMany(r => r.TournamentFixtures)
                  .HasForeignKey(x => x.RoundId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.HomeTeam)
                  .WithMany()
                  .HasForeignKey(x => x.HomeTeamId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AwayTeam)
                  .WithMany()
                  .HasForeignKey(x => x.AwayTeamId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WinnerTeam)
                  .WithMany()
                  .HasForeignKey(x => x.WinnerTeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(tf => tf.RoundId);
            entity.HasIndex(tf => tf.GroupId);
            entity.HasIndex(tf => new { tf.HomeTeamId, tf.AwayTeamId });
            entity.HasOne(x => x.Match)
                  .WithMany()
                  .HasForeignKey(x => x.MatchId)
                  .OnDelete(DeleteBehavior.Restrict);
         }
    }
}
