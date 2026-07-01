using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Scouter;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class ScouterFollowConfiguration : BaseEntityConfiguration<ScouterFollow>
    {
        public override void Configure(EntityTypeBuilder<ScouterFollow> builder)
        {
            base.Configure(builder);

            builder.HasOne(sf => sf.Scouter)
                .WithMany(s => s.ScouterFollows)
                .HasForeignKey(sf => sf.ScouterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sf => sf.Player)
                .WithMany()
                .HasForeignKey(sf => sf.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
