using Koralytics.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    internal class AIReportConfiguration : BaseEntityConfiguration<AIReport>
    {
        public override void Configure(EntityTypeBuilder<AIReport> builder)
        {
            base.Configure(builder);
            builder.Property(x => x.ReportType)
                 .IsRequired()
                 .HasConversion<string>() 
                 .HasMaxLength(50);

            builder.Property(x => x.ReferenceId)
                .IsRequired();

            builder.Property(x => x.ReportText)
                .IsRequired();

            builder.HasOne(x => x.Academy)
                .WithMany() 
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.AcademyId);
            builder.HasIndex(x => x.ReferenceId);
        }
    }
}
