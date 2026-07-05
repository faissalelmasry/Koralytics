using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Tournment
{
    public class TournamentHallOfFameConfiguration : IEntityTypeConfiguration<TournamentHallOfFame>
    {
        public void Configure(EntityTypeBuilder<TournamentHallOfFame> builder)
        {
            builder.Property(x => x.AwardType)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasOne(x => x.Tournament)
                .WithMany(t => t.TournamentHallOfFames)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Player)
                .WithMany()
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TournamentId, x.PlayerId, x.AwardType })
                .IsUnique();
        }
    }
}