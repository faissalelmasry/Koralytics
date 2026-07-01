using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Infrastructure.EntitiesConfigurations.Common;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Coach
{
    public class CoachTeamConfiguration : BaseEntityConfiguration<CoachTeam>
    {
        public override void Configure(EntityTypeBuilder<CoachTeam> builder)
        {
            base.Configure(builder);

            builder.HasOne(ct => ct.Coach)
                .WithMany(c => c.CoachTeams)
                .HasForeignKey(ct => ct.CoachUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ct => ct.Team)
                .WithMany()
                .HasForeignKey(ct => ct.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
