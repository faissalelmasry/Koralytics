using Koralytics.Domain.Entities.Parents;
using Koralytics.Infrastructure.EntitiesConfigurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Parents
{
    public class ParentPlayerJoinRequestConfiguration : AuditableEntityConfiguration<ParentPlayerJoinRequest>
    {
        public override void Configure(EntityTypeBuilder<ParentPlayerJoinRequest> builder)
        {
            base.Configure(builder);

            builder.ToTable("ParentPlayerJoinRequests");

            builder.HasOne(r => r.Parent)
                .WithMany()
                .HasForeignKey(r => r.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Player)
                .WithMany()
                .HasForeignKey(r => r.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
