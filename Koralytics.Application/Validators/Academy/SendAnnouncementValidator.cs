using FluentValidation;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Validators.Academies
{
    public class SendAnnouncementValidator : AbstractValidator<SendAnnouncementDto>
    {
        public SendAnnouncementValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Announcement title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Announcement body is required.")
                .MaximumLength(2000).WithMessage("Body must not exceed 2000 characters.");

            RuleFor(x => x.TargetType)
                .IsInEnum()
                .WithMessage("Invalid target type. Must be All, Team, AgeGroup, or Role.");

            // When targeting All, TargetId must be 0 (no specific entity)
            RuleFor(x => x.TargetId)
                .Equal(0)
                .WithMessage("TargetId must be 0 when TargetType is All.")
                .When(x => x.TargetType == AnnouncementTargetType.All);

            // When targeting a specific entity, TargetId must be a positive integer
            RuleFor(x => x.TargetId)
                .GreaterThan(0)
                .WithMessage("TargetId must be a valid positive Id when TargetType is not All.")
                .When(x => x.TargetType != AnnouncementTargetType.All);
        }
    }
}
