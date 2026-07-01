using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Player;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Player
{
    public class PlayerAcademyConfiguration : IEntityTypeConfiguration<PlayerAcademy>
    {
        public void Configure(EntityTypeBuilder<PlayerAcademy> builder)
        {
            builder.HasOne(pa => pa.Player)
                   .WithMany(p => p.PlayerAcademies)
                   .HasForeignKey(pa => pa.PlayerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pa => pa.Academy)
                   .WithMany()
                   .HasForeignKey(pa => pa.AcademyId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pa => new { pa.PlayerId, pa.AcademyId, pa.LeftAt });
        }
    }
}
