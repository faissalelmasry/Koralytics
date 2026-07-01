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
    public class PlayerConfiguration : IEntityTypeConfiguration<Domain.Entities.Player.Player>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.Player.Player> builder)
        {
            builder.ToTable("Players");
            builder.Property(p => p.PlayStyleTag).HasMaxLength(100);
            builder.Property(p => p.ArchetypePlayerName).HasMaxLength(100);
            builder.Property(p => p.ArchetypeText).HasMaxLength(1000);
            builder.Property(p => p.WeakFootRating).HasDefaultValue(3);
            builder.ToTable(t => t.HasCheckConstraint("CK_Player_WeakFoot", "[WeakFootRating] BETWEEN 1 AND 5"));
        }
    }
}
