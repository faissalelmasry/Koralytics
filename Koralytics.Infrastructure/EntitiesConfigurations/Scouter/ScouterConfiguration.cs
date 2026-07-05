using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Scouter;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Scouter
{
    public class ScouterConfiguration : IEntityTypeConfiguration<Domain.Entities.Scouter.Scouter>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.Scouter.Scouter> builder)
        {
            builder.ToTable("Scouters");
        }
    }
}
