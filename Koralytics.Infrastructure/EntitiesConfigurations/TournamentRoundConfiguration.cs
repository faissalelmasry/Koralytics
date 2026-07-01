using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class TournamentRoundConfiguration : IEntityTypeConfiguration<TournamentRound>
    {
        public void Configure(EntityTypeBuilder<TournamentRound> builder)
        {
            builder.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.RoundNumber)
                .IsRequired();

            builder.HasOne(x => x.Tournament)
                .WithMany(t => t.TournamentRounds)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TournamentId);

            builder.HasIndex(x => new { x.TournamentId, x.RoundNumber })
                .IsUnique();
        }
    }
}
