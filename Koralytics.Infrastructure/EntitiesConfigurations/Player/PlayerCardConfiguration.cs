using Koralytics.Domain.Entities.Player;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Player
{
    public class PlayerCardConfiguration : IEntityTypeConfiguration<PlayerCard>
    {
        public void Configure(EntityTypeBuilder<PlayerCard> builder)
        {
            builder.ToTable("PlayerCards");

            builder.Property(pc => pc.OverallRating)
                .IsRequired()
                .HasColumnType("decimal(5,2)");

            builder.Property(pc => pc.OverallTrainingAvg)
                .IsRequired()
                .HasColumnType("decimal(5,2)");

            builder.Property(pc => pc.OverallTournamentAvg)
                .IsRequired()
                .HasColumnType("decimal(5,2)");

            builder.Property(pc => pc.TransferClassification)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(pc => pc.LastCalculatedAt)
                .IsRequired();

            builder.HasOne(pc => pc.Player)
                .WithMany()
                .HasForeignKey(pc => pc.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(pc => pc.CategoryRatings)
                .WithOne(cr => cr.PlayerCard)
                .HasForeignKey(cr => cr.PlayerCardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(pc => pc.PlayerId)
                .IsUnique();

            builder.HasIndex(pc => pc.NeedsRecalculation);
        }
    }
}
