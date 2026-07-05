using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Player;
using Koralytics.Infrastructure.EntitiesConfigurations.Common;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Player
{
    public class ScouterViewConfiguration : BaseEntityConfiguration<ScouterView>
    {
        public override void Configure(EntityTypeBuilder<ScouterView> builder)
        {
            base.Configure(builder);

            builder.HasOne(sv => sv.Player)
                .WithMany(p => p.ScouterViews)
                .HasForeignKey(sv => sv.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sv => sv.Scouter)
                .WithMany()
                .HasForeignKey(sv => sv.ScouterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
