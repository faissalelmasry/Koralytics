using Koralytics.Domain.Entities.Match;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Match
{
    public class MatchPlayerCategoryRatingConfiguration
        : IEntityTypeConfiguration<MatchPlayerCategoryRating>
    {
        public void Configure(EntityTypeBuilder<MatchPlayerCategoryRating> builder)
        {
            builder.ToTable("MatchPlayerCategoryRatings");

            builder.Property(cr => cr.Rating)
                .IsRequired()
                .HasColumnType("decimal(4,2)");

            builder.HasOne(cr => cr.MatchPlayerRating)
                .WithMany(mpr => mpr.CategoryRatings)
                .HasForeignKey(cr => cr.MatchPlayerRatingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cr => cr.DrillCategory)
                .WithMany()
                .HasForeignKey(cr => cr.DrillCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(cr => new { cr.MatchPlayerRatingId, cr.DrillCategoryId })
                .IsUnique();

            builder.HasIndex(cr => cr.DrillCategoryId);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_MatchPlayerCategoryRating_Rating",
                    "[Rating] >= 0 AND [Rating] <= 10");
            });
        }
    }
}
