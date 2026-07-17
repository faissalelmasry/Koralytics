using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Infrastructure.EntitiesConfigurations.Common;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Coach
{
    public class CoachTempAccessConfiguration : BaseEntityConfiguration<CoachTempAccess>
    {
        public override void Configure(EntityTypeBuilder<CoachTempAccess> builder)
        {
            base.Configure(builder);

            builder.Property(cta => cta.AccessLevel)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(cta => cta.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasOne(cta => cta.Coach)
                .WithMany(c => c.CoachTempAccesses)
                .HasForeignKey(cta => cta.CoachUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cta => cta.GrantedToUser)
                .WithMany()
                .HasForeignKey(cta => cta.GrantedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
