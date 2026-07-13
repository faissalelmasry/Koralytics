using FluentValidation;
using Koralytics.Application.DTOs.Match;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Validators.Match
{
    public class SubmitLineupValidator : AbstractValidator<SubmitLineupDto>
    {
        public SubmitLineupValidator()
        {
            RuleFor(x => x.Players)
                .NotEmpty().WithMessage("Lineup must contain at least one player.");

            RuleForEach(x => x.Players).ChildRules(player =>
            {
                player.RuleFor(p => p.PlayerId)
                    .GreaterThan(0).WithMessage("PlayerId must be greater than 0.");

                player.RuleFor(p => p.TeamId)
                    .GreaterThan(0).WithMessage("TeamId must be greater than 0.");

                player.RuleFor(p => p.JerseyNumber)
                    .InclusiveBetween(1, 99)
                    .When(p => p.JerseyNumber.HasValue)
                    .WithMessage("Jersey number must be between 1 and 99.");
            });
        }
    }
}
