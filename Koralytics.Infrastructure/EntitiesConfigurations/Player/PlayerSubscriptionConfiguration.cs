using Koralytics.Domain.Entities.Player;
using Koralytics.Infrastructure.EntitiesConfigurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Player
{
    internal class PlayerSubscriptionConfiguration : BaseEntityConfiguration<PlayerSubscription>
    {
        public override void Configure(EntityTypeBuilder<PlayerSubscription> builder)
        {
            base.Configure(builder);

            // Decimal precision & scale for financial accuracy
            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Cycle dates configuration
            builder.Property(x => x.StartDate)
                .IsRequired();

            builder.Property(x => x.DueDate)
                .IsRequired();

            builder.Property(x => x.PaidAt)
                .IsRequired(false);

            builder.Property(x => x.GraceUntil)
                .IsRequired(false);

            builder.Property(x => x.PaidByUserId)
                .IsRequired(false);

            // Relationships
            builder.HasOne(x => x.Player)
                .WithMany(p => p.PlayerSubscriptions)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Academy)
                .WithMany()
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PaidByUser)
                .WithMany()
                .HasForeignKey(x => x.PaidByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Database Indexes
            builder.HasIndex(x => x.PlayerId);
            builder.HasIndex(x => x.AcademyId);
            builder.HasIndex(x => x.PaidByUserId);

            // Table constraints
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_PlayerSubscription_GraceUntil_After_PaidAt",
                    "[GraceUntil] IS NULL OR [PaidAt] IS NULL OR [GraceUntil] >= [PaidAt]");
            });
        }
    }
}