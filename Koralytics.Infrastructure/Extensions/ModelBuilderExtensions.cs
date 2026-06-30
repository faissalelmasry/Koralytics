using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Interfaces;
using Koralytics.Domain.Models.BaseModels;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyGlobalQueryFilters(this ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(e =>
                    typeof(ISoftDelete).IsAssignableFrom(e.ClrType));

            foreach (var entityType in entityTypes)
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");

                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));

                var condition = Expression.Equal(property, Expression.Constant(false));

                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
