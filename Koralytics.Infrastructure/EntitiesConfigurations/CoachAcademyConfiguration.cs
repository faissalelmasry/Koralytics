using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Coach;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class CoachAcademyConfiguration : BaseEntityConfiguration<CoachAcademy>
    {
        public override void Configure(EntityTypeBuilder<CoachAcademy> builder)
        {
            base.Configure(builder);

            builder.HasOne(ca => ca.Coach)
                .WithMany(c => c.CoachAcademies)
                .HasForeignKey(ca => ca.CoachUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ca => ca.Academy)
                .WithMany()
                .HasForeignKey(ca => ca.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
