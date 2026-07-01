using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Coach;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class CoachConfiguration : IEntityTypeConfiguration<Coach>
    {
        public void Configure(EntityTypeBuilder<Coach> builder)
        {
            builder.ToTable("Coaches");
        }
    }
}
