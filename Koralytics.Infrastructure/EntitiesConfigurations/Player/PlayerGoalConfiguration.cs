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
    internal class PlayerGoalConfiguration : BaseEntityConfiguration<PlayerGoal>
    {
        public override void Configure(EntityTypeBuilder<PlayerGoal> builder)
        {
            base.Configure(builder);
            builder.Property(x => x.Category)
                 .HasMaxLength(100)
                 .IsRequired();

            builder.Property(x => x.TargetScore)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Deadline)
                .IsRequired();

            builder.Property(x => x.Achieved)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(x => x.Academy)
                .WithMany()
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Player)
       .WithMany(p => p.PlayerGoals) 
       .HasForeignKey(x => x.PlayerId)
       .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PlayerId); builder.HasIndex(x => x.AcademyId);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_PlayerGoal_TargetScore_Positive",
                    "[TargetScore] >= 0");
            });
        }
    }
}
