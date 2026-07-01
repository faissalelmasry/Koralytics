using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Koralytics.Domain.Entities.Match;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Match
{
    public class MatchConfiguration : IEntityTypeConfiguration<Domain.Entities.Match.Match>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.Match.Match> builder)
        {
            builder.Property(m => m.Type)
               .IsRequired()
               .HasConversion<string>();

            builder.Property(m => m.Format)
                   .IsRequired()
                   .HasConversion<string>();

            builder.Property(m => m.Status)
                   .IsRequired()
                   .HasConversion<string>();

            builder.Property(m => m.Location)
                   .HasMaxLength(200);

            builder.Property(m => m.HomeScore)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(m => m.AwayScore)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.HasOne(x => x.HomeTeam)
                .WithMany()
                .HasForeignKey(x => x.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AwayTeam)
                .WithMany()
                .HasForeignKey(x => x.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.WinningTeam)
                .WithMany()
                .HasForeignKey(x => x.WinningTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tournament)
                .WithMany()
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.CreatedByUser)
               .WithMany()
               .HasForeignKey(m => m.CreatedById)
               .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(m => m.Status);

            builder.HasIndex(m => new { m.HomeTeamId, m.AwayTeamId, m.MatchDate });

            builder.HasIndex(m => m.TournamentId);

            builder.HasIndex(m => m.MatchDate);

            builder.ToTable("Matches", t =>
            {
                t.HasCheckConstraint(
                    "CK_Match_WinningTeam",
                    "[WinningTeamId] IS NULL OR [WinningTeamId] = [HomeTeamId] OR [WinningTeamId] = [AwayTeamId]"
                );
                t.HasCheckConstraint(
                    "CK_Match_PenaltyScores",
                    "([HomePenaltyScore] IS NULL AND [AwayPenaltyScore] IS NULL) OR ([HomePenaltyScore] IS NOT NULL AND [AwayPenaltyScore] IS NOT NULL)"
                );
                t.HasCheckConstraint(
                    "CK_Match_Scores",
                    "[HomeScore] >= 0 AND [AwayScore] >= 0"
                );
            });
        }
    }
}
