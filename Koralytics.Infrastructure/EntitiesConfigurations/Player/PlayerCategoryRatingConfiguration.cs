using Koralytics.Domain.Entities.Player;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Player
{
    public class PlayerCategoryRatingConfiguration
        : IEntityTypeConfiguration<PlayerCategoryRating>
    {
        public void Configure(EntityTypeBuilder<PlayerCategoryRating> builder)
        {
            builder.ToTable("PlayerCategoryRatings");

            builder.Property(pcr => pcr.Score)
                .IsRequired()
                .HasColumnType("decimal(5,2)");

            builder.Property(pcr => pcr.LastUpdatedAt)
                .IsRequired();

            builder.HasOne(pcr => pcr.PlayerCard)
                .WithMany(pc => pc.CategoryRatings)
                .HasForeignKey(pcr => pcr.PlayerCardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pcr => pcr.DrillCategory)
                .WithMany()
                .HasForeignKey(pcr => pcr.DrillCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pcr => new { pcr.PlayerCardId, pcr.DrillCategoryId })
                .IsUnique();
        }
    }
}
