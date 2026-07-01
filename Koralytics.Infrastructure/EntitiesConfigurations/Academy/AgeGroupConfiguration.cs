using Koralytics.Domain.Entities.Academy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Academy
{
    public class AgeGroupConfiguration : IEntityTypeConfiguration<AgeGroup>
    {
        public void Configure(EntityTypeBuilder<AgeGroup> builder)
        {
            builder.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.MinAge)
                .IsRequired();

            builder.Property(x => x.MaxAge)
                .IsRequired();

            // Relationships
            builder.HasOne(x => x.Academy)
                .WithMany(a => a.AgeGroups)
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);

            //builder.HasMany(x => x.Teams)
            //    .WithOne(t => t.AgeGroup)
            //    .HasForeignKey(t => t.AgeGroupId)
            //    .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.AcademyId);

            builder.HasIndex(x => new { x.AcademyId, x.Name })
                .IsUnique();

            // Check Constraints
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_AgeGroup_Ages",
                    "[MaxAge] > [MinAge]");

                t.HasCheckConstraint(
                    "CK_AgeGroup_MinAge",
                    "[MinAge] >= 5");

                t.HasCheckConstraint(
                    "CK_AgeGroup_MaxAge",
                    "[MaxAge] <= 50");
            });
        }
    }
}