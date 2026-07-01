using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Player;

namespace Koralytics.Infrastructure.EntitiesConfigurations
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
