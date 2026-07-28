using Koralytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.Persistence.Configurations
{
    public class AcademyPlanConfiguration : IEntityTypeConfiguration<AcademyPlan>
    {
        public void Configure(EntityTypeBuilder<AcademyPlan> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                   .HasColumnType("decimal(18,2)");

            builder.HasOne(p => p.Academy)
                   .WithMany(a => a.Plans)
                   .HasForeignKey(p => p.AcademyId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}