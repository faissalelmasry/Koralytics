using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class TournamentGroupTeamConfiguration : IEntityTypeConfiguration<TournamentGroupTeam>
    {
        public void Configure(EntityTypeBuilder<TournamentGroupTeam> builder)
        {
            builder.HasOne(x => x.Group)
                .WithMany(g => g.TournamentGroupTeams)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.TournamentTeam)
                .WithMany(tt => tt.TournamentGroupTeams)
                .HasForeignKey(x => x.TournamentTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.GroupId, x.TournamentTeamId })
                .IsUnique();
        }
    }
}