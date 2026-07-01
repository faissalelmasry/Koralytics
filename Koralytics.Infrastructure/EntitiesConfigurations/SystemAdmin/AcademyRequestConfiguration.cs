using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.SystemAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.SystemAdmin
{
    public class AcademyRequestConfiguration : IEntityTypeConfiguration<AcademyRequest>
    {
        public void Configure(EntityTypeBuilder<AcademyRequest> builder)
        {
            builder.ToTable("AcademyRequests");

            builder.Property(ar => ar.AcademyName)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(ar => ar.ContactPersonName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ar => ar.ContactEmail)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(ar => ar.ContactPhone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(ar => ar.Location)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(ar => ar.RequestedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(ar => ar.RequestStatus)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(ar => ar.RejectedReason)
                .HasMaxLength(1000);

            builder.HasOne(ar => ar.RequestedBy)
                .WithMany()
                .HasForeignKey(ar => ar.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ar => ar.ReviewedBy)
                .WithMany()
                .HasForeignKey(ar => ar.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(ar => ar.RequestStatus);
            builder.HasIndex(ar => ar.RequestedAt);
            builder.HasIndex(ar => ar.RequestedById);
            builder.HasIndex(ar => ar.ReviewedById);
            builder.HasIndex(ar => new { ar.RequestStatus, ar.RequestedAt });
        }
    }
}
