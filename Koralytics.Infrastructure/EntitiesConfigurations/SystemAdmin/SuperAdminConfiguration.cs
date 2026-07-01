using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.SystemAdmin;

namespace Koralytics.Infrastructure.EntitiesConfigurations.SystemAdmin
{
    public class SystemAdminConfiguration : IEntityTypeConfiguration<SystemAdminUser>
    {
        public void Configure(EntityTypeBuilder<SystemAdminUser> builder)
        {
        }
    }
}
