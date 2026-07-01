using Koralytics.Domain.Entities.Academy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class AcademyConfiguration : IEntityTypeConfiguration<Academy>
    {
        public void Configure(EntityTypeBuilder<Academy> builder)
        {
            builder.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.LogoUrl)
                .HasMaxLength(500);

            builder.Property(x => x.PrimaryColor)
                .HasMaxLength(7);

            builder.Property(x => x.SecondaryColor)
                .HasMaxLength(7);

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.FoundedAt)
                .IsRequired();

            // Relationships
            builder.HasOne(x => x.Admin)
                .WithMany()
                .HasForeignKey(x => x.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.AgeGroups)
                .WithOne(a => a.Academy)
                .HasForeignKey(a => a.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);

            //builder.HasMany(x => x.AcademyLocations)
            //    .WithOne(a => a.Academy)
            //    .HasForeignKey(a => a.AcademyId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //builder.HasMany(x => x.AcademyAnnouncements)
            //    .WithOne(a => a.Academy)
            //    .HasForeignKey(a => a.AcademyId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //builder.HasMany(x => x.AcademyBadges)
            //    .WithOne(a => a.Academy)
            //    .HasForeignKey(a => a.AcademyId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //builder.HasMany(x => x.RoleAuditLogs)
            //    .WithOne(a => a.Academy)
            //    .HasForeignKey(a => a.AcademyId)
            //    .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.HasIndex(x => x.AdminUserId);

            builder.HasIndex(x => x.Status);

            // Check Constraints
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Academy_FoundedAt",
                    "[FoundedAt] <= GETUTCDATE()");
            });
        }
    }
}