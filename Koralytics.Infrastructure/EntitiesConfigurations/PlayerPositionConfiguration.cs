using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Player;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class PlayerPositionConfiguration : BaseEntityConfiguration<PlayerPosition>
    {
        public override void Configure(EntityTypeBuilder<PlayerPosition> builder)
        {
            base.Configure(builder);

            builder.HasOne(pp => pp.Player)
                .WithMany(p => p.PlayerPositions)
                .HasForeignKey(pp => pp.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
