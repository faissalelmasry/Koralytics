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
    public class TournamentFixtureConfiguration : IEntityTypeConfiguration<TournamentFixture>
    {
        public void Configure(EntityTypeBuilder<TournamentFixture> entity)
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Tournament)
                  .WithMany()
                  .HasForeignKey(x => x.TournamentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Group)
                  .WithMany()
                  .HasForeignKey(x => x.GroupId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Round)
                  .WithMany()
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
        }
    }
}
