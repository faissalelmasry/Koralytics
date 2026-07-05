using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Tournment
{
    public class TournamentStandingConfiguration : IEntityTypeConfiguration<TournamentStanding>
    {
        public void Configure(EntityTypeBuilder<TournamentStanding> builder)
        {
            builder.HasOne(x => x.Group)
                .WithMany(g => g.TournamentStandings)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.TournamentTeam)
                .WithMany(tt => tt.TournamentStandings)
                .HasForeignKey(x => x.TournamentTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.GroupId, x.TournamentTeamId })
                .IsUnique();

            builder.Ignore(x => x.GoalDifference);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Standing_Points", "[Points] >= 0");
                t.HasCheckConstraint("CK_Standing_Played", "[Played] >= 0");
                t.HasCheckConstraint("CK_Standing_Won", "[Won] >= 0");
                t.HasCheckConstraint("CK_Standing_Drawn", "[Drawn] >= 0");
                t.HasCheckConstraint("CK_Standing_Lost", "[Lost] >= 0");
            });
        }
    }
}