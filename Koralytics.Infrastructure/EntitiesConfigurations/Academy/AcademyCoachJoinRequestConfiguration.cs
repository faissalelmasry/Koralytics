using Koralytics.Domain.Entities.Academy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Academy
{
    public class AcademyCoachJoinRequestConfiguration : IEntityTypeConfiguration<AcademyCoachJoinRequest>
    {
        public void Configure(EntityTypeBuilder<AcademyCoachJoinRequest> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Status)
                .IsRequired();

            builder.Property(r => r.RequestedAt)
                .IsRequired();

            builder.HasOne(r => r.Academy)
                .WithMany()
                .HasForeignKey(r => r.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Coach)
                .WithMany()
                .HasForeignKey(r => r.CoachId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
