using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Tournment
{
    public class TournamentTeamConfiguration : IEntityTypeConfiguration<TournamentTeam>
    {
        public void Configure(EntityTypeBuilder<TournamentTeam> builder)
        {
            builder.HasOne(x => x.Tournament)
                .WithMany(t => t.TournamentTeams)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.HomeFixtures)
                .WithOne(f => f.HomeTeam)
                .HasForeignKey(f => f.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.AwayFixtures)
                .WithOne(f => f.AwayTeam)
                .HasForeignKey(f => f.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TournamentId, x.TeamId })
                .IsUnique();

            builder.HasIndex(x => x.Status);
        }
    }
}