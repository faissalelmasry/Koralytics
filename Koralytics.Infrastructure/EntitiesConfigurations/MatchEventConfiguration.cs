using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Match;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class MatchEventConfiguration : IEntityTypeConfiguration<MatchEvent>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<MatchEvent> builder)
        {
            builder.Property(x => x.Minute)
            .IsRequired();

            builder.Property(x => x.EventType)
                .IsRequired();

            builder.HasIndex(x => new { x.MatchId, x.PlayerId });

            builder.HasIndex(me => me.MatchId);

            builder.HasIndex(me => new { me.MatchId, me.EventType });

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_MatchEvent_Minute",
                    "[Minute] >= 0 AND [Minute] <= 130");

                t.HasCheckConstraint(
                    "CK_MatchEvent_Player_AssistPlayer",
                    "[AssistPlayerId] IS NULL OR [PlayerId] <> [AssistPlayerId]");
            });

            builder.HasOne(x => x.Match)
                .WithMany(x => x.MatchEvents)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Player)
                .WithMany()
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AssistPlayer)
                .WithMany()
                .HasForeignKey(x => x.AssistPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(me => me.CreatedByUser)
               .WithMany()
               .HasForeignKey(me => me.CreatedById)
               .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
