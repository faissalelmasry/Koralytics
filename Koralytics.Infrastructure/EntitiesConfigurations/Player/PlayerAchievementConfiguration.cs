using Koralytics.Domain.Entities.Player;
using Koralytics.Infrastructure.EntitiesConfigurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Player
{
    internal class PlayerAchievementConfiguration : BaseEntityConfiguration<PlayerAchievement>
    {
        public override void Configure(EntityTypeBuilder<PlayerAchievement> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.AchievementType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ReferenceType)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.HasOne(x => x.Player)
                .WithMany(p=>p.PlayerAchievements) 
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PlayerId);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_PlayerAchievement_Polymorphic_Pair",
                    "([ReferenceId] IS NULL AND [ReferenceType] IS NULL) OR ([ReferenceId] IS NOT NULL AND [ReferenceType] IS NOT NULL)");
            });
        }
    }
}
