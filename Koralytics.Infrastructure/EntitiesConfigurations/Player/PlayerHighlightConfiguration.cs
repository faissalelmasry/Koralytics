using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Player;
using Koralytics.Infrastructure.EntitiesConfigurations.Common;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Player
{
    public class PlayerHighlightConfiguration : BaseEntityConfiguration<PlayerHighlight>
    {
        public override void Configure(EntityTypeBuilder<PlayerHighlight> builder)
        {
            base.Configure(builder);

            builder.HasOne(ph => ph.Player)
                .WithMany(p => p.PlayerHighlights)
                .HasForeignKey(ph => ph.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ph => ph.Academy)
                .WithMany()
                .HasForeignKey(ph => ph.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
