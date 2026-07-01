using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Scouter;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class ScouterReportConfiguration : BaseEntityConfiguration<ScouterReport>
    {
        public override void Configure(EntityTypeBuilder<ScouterReport> builder)
        {
            base.Configure(builder);

            builder.HasOne(sr => sr.Scouter)
                .WithMany(s => s.ScouterReports)
                .HasForeignKey(sr => sr.ScouterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sr => sr.Player)
                .WithMany()
                .HasForeignKey(sr => sr.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
