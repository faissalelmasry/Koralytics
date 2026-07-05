using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Tournamet;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Tournment
{
    public class TournamentGroupConfiguration : IEntityTypeConfiguration<TournamentGroup>
    {
        public void Configure(EntityTypeBuilder<TournamentGroup> builder)
        {

            builder.HasKey(tg => tg.Id);

            builder.Property(tg => tg.Name)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(tg => tg.IsDummy)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.HasOne(tg => tg.Tournament)
                   .WithMany(t => t.TournamentGroups)
                   .HasForeignKey(tg => tg.TournamentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(tg => tg.TournamentId);
        }
    }
}
