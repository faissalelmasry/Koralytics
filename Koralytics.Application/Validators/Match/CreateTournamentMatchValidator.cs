using FluentValidation;
using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Validators.Match
{
    public class CreateTournamentMatchValidator : AbstractValidator<CreateTournamentMatchDto>
    {
        public CreateTournamentMatchValidator()
        {
            RuleFor(x => x.TournamentFixtureId)
                .GreaterThan(0).WithMessage("TournamentFixtureId must be greater than 0.");

            RuleFor(x => x.HomeTeamId)
                .GreaterThan(0).WithMessage("HomeTeamId must be greater than 0.");

            RuleFor(x => x.AwayTeamId)
                .GreaterThan(0).WithMessage("AwayTeamId must be greater than 0.");

            RuleFor(x => x)
                .Must(x => x.HomeTeamId != x.AwayTeamId)
                .WithMessage("Home team and away team must be different.");

            RuleFor(x => x.Format)
                .IsInEnum().WithMessage("Invalid match format.");

            RuleFor(x => x.MatchDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Match date must be in the future.");

            RuleFor(x => x.Location)
                .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");
        }
    }
}
