using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Scouter;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class ScouterShortlistConfiguration : BaseEntityConfiguration<ScouterShortlist>
    {
        public override void Configure(EntityTypeBuilder<ScouterShortlist> builder)
        {
            base.Configure(builder);

            builder.HasOne(ss => ss.Scouter)
                .WithMany(s => s.ScouterShortlists)
                .HasForeignKey(ss => ss.ScouterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ss => ss.Player)
                .WithMany()
                .HasForeignKey(ss => ss.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
