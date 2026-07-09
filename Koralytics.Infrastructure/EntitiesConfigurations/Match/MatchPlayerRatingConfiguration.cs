using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Match;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Match
{
    public class MatchPlayerRatingConfiguration : IEntityTypeConfiguration<MatchPlayerRating>
    {
        public void Configure(EntityTypeBuilder<MatchPlayerRating> builder)
        {

            builder.Ignore(mpr => mpr.Rating);

            builder.Property(mpr => mpr.Goals)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(mpr => mpr.Assists)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(mpr => mpr.MinutesPlayed)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(mpr => mpr.IsMOTM)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(mpr => mpr.CoachNote)
                   .HasMaxLength(1000);


            builder.HasOne(x => x.Match)
                .WithMany(x => x.MatchPlayerRatings)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Player)
                .WithMany(p => p.PlayerRatings)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Coach)
                .WithMany()
                .HasForeignKey(x => x.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.CategoryRatings)
                .WithOne(cr => cr.MatchPlayerRating)
                .HasForeignKey(cr => cr.MatchPlayerRatingId)
                .OnDelete(DeleteBehavior.Cascade);
            builder
                .HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedById);


            builder.HasIndex(mpr => new { mpr.MatchId, mpr.PlayerId })
               .IsUnique();

            builder.HasIndex(mpr => new { mpr.PlayerId, mpr.IsMOTM });

            builder.HasIndex(mpr => mpr.MatchId);


            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_MatchPlayerRating_Goals",
                    "[Goals] >= 0");

                t.HasCheckConstraint(
                    "CK_MatchPlayerRating_Assists",
                    "[Assists] >= 0");

                t.HasCheckConstraint(
                    "CK_MatchPlayerRating_MinutesPlayed",
                    "[MinutesPlayed] >= 0 AND [MinutesPlayed] <= 150");
            });
        }
    }
}
