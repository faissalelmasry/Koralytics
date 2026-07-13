using FluentValidation;
using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Validators.Match
{
    public class SubmitMatchRatingsValidator : AbstractValidator<SubmitMatchRatingsDto>
    {
        public SubmitMatchRatingsValidator()
        {
            RuleFor(x => x.Ratings)
                .NotEmpty().WithMessage("At least one rating must be provided.");

            RuleForEach(x => x.Ratings).ChildRules(rating =>
            {
                rating.RuleFor(r => r.PlayerId)
                    .GreaterThan(0).WithMessage("PlayerId must be greater than 0.");

                rating.RuleFor(r => r.MinutesPlayed)
                    .InclusiveBetween(0, 150).WithMessage("Minutes played must be between 0 and 150.");

                rating.RuleFor(r => r.CategoryRatings)
                    .NotEmpty().WithMessage("At least one category rating must be provided.");

                rating.RuleFor(r => r.CoachNote)
                    .MaximumLength(1000)
                    .When(r => !string.IsNullOrEmpty(r.CoachNote))
                    .WithMessage("Coach note must not exceed 1000 characters.");

                rating.RuleForEach(r => r.CategoryRatings).ChildRules(cr =>
                {
                    cr.RuleFor(c => c.DrillCategoryId)
                        .GreaterThan(0).WithMessage("DrillCategoryId must be greater than 0.");

                    cr.RuleFor(c => c.Rating)
                        .InclusiveBetween(0, 10).WithMessage("Rating must be between 0 and 10.");
                });
            });
        }
    }
}
