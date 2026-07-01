using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Coach;

namespace Koralytics.Infrastructure.EntitiesConfigurations.Coach
{
    public class CoachConfiguration : IEntityTypeConfiguration<Domain.Entities.Coach.Coach>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.Coach.Coach> builder)
        {
            builder.ToTable("Coaches");
        }
    }
}
