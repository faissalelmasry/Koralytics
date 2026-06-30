using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Infrastructure.EntitiesConfigurations;
using Koralytics.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.Context
{
    public class ApplicationDbContext:IdentityDbContext<User,Role, int>
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.ApplyGlobalQueryFilters();
        }
    }
}
