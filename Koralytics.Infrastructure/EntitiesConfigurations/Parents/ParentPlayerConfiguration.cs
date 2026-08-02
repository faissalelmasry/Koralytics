using Koralytics.Domain.Entities.Parents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Parents
{
    public class ParentPlayerConfiguration : IEntityTypeConfiguration<ParentPlayer>
    {
        public void Configure(EntityTypeBuilder<ParentPlayer> builder)
        {
            builder.ToTable("ParentPlayers");

            builder.HasKey(pp => new { pp.ParentId, pp.PlayerId });

            builder.Property(pp => pp.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            builder.HasOne(pp => pp.Parent)
                .WithMany()
                .HasForeignKey(pp => pp.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pp => pp.Player)
                .WithMany(p => p.ParentPlayers)
                .HasForeignKey(pp => pp.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pp => new { pp.ParentId, pp.PlayerId })
                .IsUnique();
        }
    }
}
