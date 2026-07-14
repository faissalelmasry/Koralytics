using Koralytics.Domain.Entities.Match;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Match
{
    public class MatchRequestConfiguration : IEntityTypeConfiguration<MatchRequest>
    {
        public void Configure(EntityTypeBuilder<MatchRequest> builder)
        {
            builder.Property(r => r.Format)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.Location)
                .HasMaxLength(200);

            builder.HasOne(r => r.RequesterTeam)
                .WithMany()
                .HasForeignKey(r => r.RequesterTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.TargetTeam)
                .WithMany()
                .HasForeignKey(r => r.TargetTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.RequesterCoach)
                .WithMany()
                .HasForeignKey(r => r.RequesterCoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ResolvedByCoach)
                .WithMany()
                .HasForeignKey(r => r.ResolvedByCoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Match)
                .WithMany()
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => r.RequesterTeamId);
            builder.HasIndex(r => r.TargetTeamId);
        }
    }
}
