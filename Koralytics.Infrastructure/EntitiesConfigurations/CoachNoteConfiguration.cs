using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Koralytics.Domain.Entities.Coach;

namespace Koralytics.Infrastructure.EntitiesConfigurations
{
    public class CoachNoteConfiguration : BaseEntityConfiguration<CoachNote>
    {
        public override void Configure(EntityTypeBuilder<CoachNote> builder)
        {
            base.Configure(builder);

            builder.HasOne(cn => cn.Coach)
                .WithMany(c => c.CoachNotes)
                .HasForeignKey(cn => cn.CoachUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cn => cn.Player)
                .WithMany()
                .HasForeignKey(cn => cn.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cn => cn.Academy)
                .WithMany()
                .HasForeignKey(cn => cn.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cn => cn.Session)
                .WithMany()
                .HasForeignKey(cn => cn.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cn => cn.Match)
                .WithMany()
                .HasForeignKey(cn => cn.MatchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
