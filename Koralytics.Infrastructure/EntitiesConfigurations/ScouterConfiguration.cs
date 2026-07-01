using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Scouter;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class ScouterConfiguration : IEntityTypeConfiguration<Scouter>
    {
        public void Configure(EntityTypeBuilder<Scouter> builder)
        {
            builder.ToTable("Scouters");
        }
    }
}
