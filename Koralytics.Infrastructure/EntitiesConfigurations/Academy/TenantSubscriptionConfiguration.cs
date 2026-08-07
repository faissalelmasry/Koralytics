using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Academy
{
    public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
    {
        public void Configure(EntityTypeBuilder<TenantSubscription> builder)
        {
            builder.ToTable("TenantSubscriptions");

            builder.HasKey(x => x.Id);

            // ── Tier stored as string (nvarchar) — consistent with project conventions ──
            builder.Property(x => x.Tier)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            // ── Status stored as string ──────────────────────────────────────────────
            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            // ── Date range ───────────────────────────────────────────────────────────
            builder.Property(x => x.StartsAt).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();

            // ── IsActive is computed in C# — no DB column ────────────────────────────
            builder.Ignore(x => x.IsActive);

            // ── Relationship: one TenantSubscription per Academy ─────────────────────
            builder.HasOne(x => x.Academy)
                   .WithOne(a => a.Subscription)
                   .HasForeignKey<TenantSubscription>(x => x.AcademyId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Unique index — enforces one active subscription per academy ──────────
            builder.HasIndex(x => x.AcademyId)
                   .IsUnique()
                   .HasDatabaseName("IX_TenantSubscriptions_AcademyId_Unique");

            // ── Supporting indexes ───────────────────────────────────────────────────
            builder.HasIndex(x => x.Tier)
                   .HasDatabaseName("IX_TenantSubscriptions_Tier");

            builder.HasIndex(x => x.ExpiresAt)
                   .HasDatabaseName("IX_TenantSubscriptions_ExpiresAt");
        }
    }
}
