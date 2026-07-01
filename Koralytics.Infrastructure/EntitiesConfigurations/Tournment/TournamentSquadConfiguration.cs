using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Tournment
{
    public class TournamentSquadConfiguration : IEntityTypeConfiguration<TournamentSquad>
    {
        public void Configure(EntityTypeBuilder<TournamentSquad> builder)
        {
            builder.HasOne(x => x.Tournament)
                .WithMany(t => t.TournamentSquads)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Player)
                .WithMany()
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TournamentId, x.PlayerId })
                .IsUnique();

            builder.HasIndex(x => new { x.TournamentId, x.TeamId });
        }
    }
}