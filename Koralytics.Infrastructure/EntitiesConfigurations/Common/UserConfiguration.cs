using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Common
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.FirstName).IsRequired().HasMaxLength(150);
            builder.Property(u => u.LastName).IsRequired().HasMaxLength(150);
            builder.Property(u => u.ProfileImageUrl).HasMaxLength(500);
            builder.HasIndex(u => u.FirstName);
            // Self-referencing relationships for audit tracking
            builder.HasOne(u => u.CreatedByUser)
                .WithMany() // no inverse navigation collection needed
                .HasForeignKey(u => u.CreatedById)
                .OnDelete(DeleteBehavior.Restrict); // avoid cascade delete cycles

            builder.HasOne(u => u.UpdatedByUser)
                .WithMany()
                .HasForeignKey(u => u.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
